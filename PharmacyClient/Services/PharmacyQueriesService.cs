using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.Services;

/// <summary>
/// Сервис для выполнения LINQ-запросов к базе данных аптеки
/// Реализует все 13 видов запросов согласно заданию варианта №19
/// </summary>
public class PharmacyQueriesService
{
    private readonly PharmacyDbContext _context;

    public PharmacyQueriesService(PharmacyDbContext context)
    {
        _context = context;
    }

    #region Запрос 1: Покупатели, не забравшие заказ вовремя
    /// <summary>
    /// Получить сведения о покупателях, которые не пришли забрать свой заказ 
    /// в назначенное им время и общее их число
    /// </summary>
    public (List<PatientInfo> Patients, int TotalCount) GetPatientsWhoDidNotPickupOrders()
    {
        var currentDate = DateTime.Now;

        var query = from order in _context.Orders
                    where order.RequiredDate != null 
                          && order.PickupDate == null 
                          && order.RequiredDate < currentDate
                          && order.OrderStatus != "Отменён"
                    join patient in _context.Patients on order.PatientId equals patient.PatientId
                    select new PatientInfo
                    {
                        PatientId = patient.PatientId,
                        FullName = $"{patient.LastName} {patient.FirstName} {patient.Patronymic}".Trim(),
                        Phone = patient.Phone,
                        Address = patient.Address,
                        OrderNumber = order.OrderNumber,
                        MedicineName = order.Medicine.MedicineName,
                        RequiredDate = order.RequiredDate.Value,
                        DaysOverdue = (currentDate - order.RequiredDate.Value).Days
                    };

        var patients = query.ToList();
        return (patients, patients.Count);
    }
    #endregion

    #region Запрос 2: Покупатели, ждущие прибытия медикаментов
    /// <summary>
    /// Получить перечень и общее число покупателей, которые ждут прибытия на склад 
    /// нужных им медикаментов в целом и по указанной категории медикаментов
    /// </summary>
    public (List<PatientWaitingInfo> Patients, int TotalCount) GetPatientsWaitingForMedicines(int? categoryId = null)
    {
        var query = from order in _context.Orders
                    where order.IsAllComponentsAvailable == false
                          && order.OrderStatus != "Выполнен"
                          && order.OrderStatus != "Отменён"
                    join patient in _context.Patients on order.PatientId equals patient.PatientId
                    join medicine in _context.Medicines on order.MedicineId equals medicine.MedicineId
                    where categoryId == null || medicine.CategoryId == categoryId
                    select new PatientWaitingInfo
                    {
                        PatientId = patient.PatientId,
                        FullName = $"{patient.LastName} {patient.FirstName} {patient.Patronymic}".Trim(),
                        Phone = patient.Phone,
                        Address = patient.Address,
                        OrderNumber = order.OrderNumber,
                        MedicineName = medicine.MedicineName,
                        CategoryName = medicine.Category != null ? medicine.Category.CategoryName : "Без категории",
                        OrderDate = order.OrderDate,
                        MissingComponentsNote = order.MissingComponentsNote
                    };

        var patients = query.ToList();
        return (patients, patients.Count);
    }
    #endregion

    #region Запрос 3: Топ-10 наиболее используемых медикаментов
    /// <summary>
    /// Получить перечень десяти наиболее часто используемых медикаментов 
    /// в целом и указанной категории медикаментов
    /// </summary>
    public List<MedicineUsageInfo> GetTop10MostUsedMedicines(int? categoryId = null)
    {
        var query = from order in _context.Orders
                    where order.OrderStatus == "Выполнен" || order.OrderStatus == "Выдан"
                    join medicine in _context.Medicines on order.MedicineId equals medicine.MedicineId
                    where categoryId == null || medicine.CategoryId == categoryId
                    group order by new { medicine.MedicineId, medicine.MedicineName, medicine.CategoryId } into g
                    orderby g.Sum(x => x.Quantity) descending
                    select new MedicineUsageInfo
                    {
                        MedicineId = g.Key.MedicineId,
                        MedicineName = g.Key.MedicineName,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        OrdersCount = g.Count(),
                        CategoryId = g.Key.CategoryId
                    };

        return query.Take(10).ToList();
    }
    #endregion

    #region Запрос 4: Объем использованных веществ за период
    /// <summary>
    /// Получить какой объем указанных веществ использован за указанный период
    /// </summary>
    public List<ComponentUsageInfo> GetComponentUsage(DateTime startDate, DateTime endDate, List<int>? componentIds = null)
    {
        var query = from order in _context.Orders
                    where order.OrderDate >= startDate && order.OrderDate <= endDate
                          && (order.OrderStatus == "Выполнен" || order.OrderStatus == "Выдан")
                    join medicineComposition in _context.MedicineCompositions on order.MedicineId equals medicineComposition.MedicineId
                    join component in _context.Components on medicineComposition.ComponentId equals component.ComponentId
                    where componentIds == null || componentIds.Contains(component.ComponentId)
                    group new { CompQuantity = medicineComposition.Quantity, OrderQuantity = order.Quantity } by new 
                    { 
                        component.ComponentId, 
                        component.ComponentName,
                        component.UnitId,
                        unit = component.Unit 
                    } into g
                    select new ComponentUsageInfo
                    {
                        ComponentId = g.Key.ComponentId,
                        ComponentName = g.Key.ComponentName,
                        UnitName = g.Key.unit.UnitName,
                        TotalQuantity = g.Sum(x => x.OrderQuantity * x.CompQuantity),
                        OrdersCount = g.Count()
                    };

        return query.ToList();
    }
    #endregion

    #region Запрос 5: Покупатели, заказывавшие определенные лекарства
    /// <summary>
    /// Получить перечень и общее число покупателей, заказывавших определенное лекарство 
    /// или определенные типы лекарств за данный период
    /// </summary>
    public (List<PatientOrderInfo> Patients, int TotalCount) GetPatientsWhoOrderedMedicines(
        DateTime startDate, DateTime endDate, 
        int? medicineId = null, 
        List<int>? medicineTypeIds = null)
    {
        var query = from order in _context.Orders
                    where order.OrderDate >= startDate && order.OrderDate <= endDate
                    join patient in _context.Patients on order.PatientId equals patient.PatientId
                    join medicine in _context.Medicines on order.MedicineId equals medicine.MedicineId
                    where medicineId == null || medicine.MedicineId == medicineId
                    where medicineTypeIds == null || medicineTypeIds.Contains(medicine.MedicineTypeId)
                    select new PatientOrderInfo
                    {
                        PatientId = patient.PatientId,
                        FullName = $"{patient.LastName} {patient.FirstName} {patient.Patronymic}".Trim(),
                        Phone = patient.Phone,
                        MedicineName = medicine.MedicineName,
                        MedicineTypeName = medicine.MedicineType.TypeName,
                        OrderDate = order.OrderDate,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice
                    };

        var patients = query.ToList();
        return (patients, patients.Count);
    }
    #endregion

    #region Запрос 6: Лекарства, достигшие критической нормы или закончившиеся
    /// <summary>
    /// Получить перечень и типы лекарств, достигших своей критической нормы или закончившихся
    /// </summary>
    public List<CriticalNormMedicineInfo> GetMedicinesAtCriticalNormOrOutOfStock()
    {
        var query = from medicine in _context.Medicines
                    where medicine.CurrentStock <= medicine.CriticalNorm
                    orderby medicine.CurrentStock ascending
                    select new CriticalNormMedicineInfo
                    {
                        MedicineId = medicine.MedicineId,
                        MedicineName = medicine.MedicineName,
                        MedicineTypeName = medicine.MedicineType.TypeName,
                        CurrentStock = medicine.CurrentStock ?? 0,
                        CriticalNorm = medicine.CriticalNorm,
                        ShortageAmount = medicine.CriticalNorm - (medicine.CurrentStock ?? 0),
                        IsOutOfStock = medicine.CurrentStock <= 0,
                        UnitName = medicine.Unit.UnitName
                    };

        return query.ToList();
    }
    #endregion

    #region Запрос 7: Лекарства с минимальным запасом на складе
    /// <summary>
    /// Получить перечень лекарств с минимальным запасом на складе 
    /// в целом и по указанной категории медикаментов
    /// </summary>
    public List<MinimumStockMedicineInfo> GetMedicinesWithMinimumStock(int? categoryId = null, int count = 10)
    {
        var query = from medicine in _context.Medicines
                    where categoryId == null || medicine.CategoryId == categoryId
                    orderby medicine.CurrentStock ascending
                    select new MinimumStockMedicineInfo
                    {
                        MedicineId = medicine.MedicineId,
                        MedicineName = medicine.MedicineName,
                        CategoryName = medicine.Category != null ? medicine.Category.CategoryName : "Без категории",
                        CurrentStock = medicine.CurrentStock ?? 0,
                        CriticalNorm = medicine.CriticalNorm,
                        UnitName = medicine.Unit.UnitName,
                        SalePrice = medicine.SalePrice
                    };

        return query.Take(count).ToList();
    }
    #endregion

    #region Запрос 8: Заказы в производстве
    /// <summary>
    /// Получить полный перечень и общее число заказов находящихся в производстве
    /// </summary>
    public (List<OrderInProductionInfo> Orders, int TotalCount) GetOrdersInProduction()
    {
        var query = from order in _context.Orders
                    where order.OrderStatus == "В производстве" || order.OrderStatus == "Готовится"
                    join patient in _context.Patients on order.PatientId equals patient.PatientId
                    join medicine in _context.Medicines on order.MedicineId equals medicine.MedicineId
                    join employee in _context.Employees on order.ProductionEmployeeId equals employee.EmployeeId into prodEmp
                    from pe in prodEmp.DefaultIfEmpty()
                    select new OrderInProductionInfo
                    {
                        OrderId = order.OrderId,
                        OrderNumber = order.OrderNumber,
                        PatientName = $"{patient.LastName} {patient.FirstName} {patient.Patronymic}".Trim(),
                        MedicineName = medicine.MedicineName,
                        OrderDate = order.OrderDate,
                        RequiredDate = order.RequiredDate,
                        ReadyDate = order.ReadyDate,
                        Quantity = order.Quantity,
                        ProductionEmployeeName = pe != null ? $"{pe.LastName} {pe.FirstName}" : "Не назначен",
                        IsAllComponentsAvailable = order.IsAllComponentsAvailable ?? false,
                        Notes = order.Notes
                    };

        var orders = query.ToList();
        return (orders, orders.Count);
    }
    #endregion

    #region Запрос 9: Препараты для заказов в производстве
    /// <summary>
    /// Получить полный перечень и общее число препаратов требующихся для заказов, 
    /// находящихся в производстве
    /// </summary>
    public (List<RequiredComponentsInfo> Components, int TotalCount) GetRequiredComponentsForProductionOrders()
    {
        var query = from order in _context.Orders
                    where order.OrderStatus == "В производстве" || order.OrderStatus == "Готовится"
                    join composition in _context.MedicineCompositions on order.MedicineId equals composition.MedicineId
                    join component in _context.Components on composition.ComponentId equals component.ComponentId
                    select new RequiredComponentsInfo
                    {
                        OrderNumber = order.OrderNumber,
                        MedicineName = order.Medicine.MedicineName,
                        ComponentName = component.ComponentName,
                        RequiredQuantity = composition.Quantity * order.Quantity,
                        UnitName = component.Unit.UnitName,
                        CurrentStock = component.CurrentStock ?? 0,
                        IsAvailable = (component.CurrentStock ?? 0) >= (composition.Quantity * order.Quantity)
                    };

        var components = query.ToList();
        return (components, components.Count);
    }
    #endregion

    #region Запрос 10: Технологии приготовления лекарств
    /// <summary>
    /// Получить все технологии приготовления лекарств указанных типов, 
    /// конкретных лекарств, лекарств, находящихся в справочнике заказов в производстве
    /// </summary>
    public List<PreparationTechnologyInfo> GetPreparationTechnologies(
        List<int>? medicineTypeIds = null,
        List<int>? medicineIds = null,
        bool onlyForProductionOrders = false)
    {
        IQueryable<Medicine> medicinesQuery = _context.Medicines;

        if (onlyForProductionOrders)
        {
            var productionMedicineIds = (from order in _context.Orders
                                         where order.OrderStatus == "В производстве" || order.OrderStatus == "Готовится"
                                         select order.MedicineId).Distinct();
            medicinesQuery = medicinesQuery.Where(m => productionMedicineIds.Contains(m.MedicineId));
        }

        if (medicineIds != null && medicineIds.Any())
        {
            medicinesQuery = medicinesQuery.Where(m => medicineIds.Contains(m.MedicineId));
        }

        if (medicineTypeIds != null && medicineTypeIds.Any())
        {
            medicinesQuery = medicinesQuery.Where(m => medicineTypeIds.Contains(m.MedicineTypeId));
        }

        var query = from medicine in medicinesQuery
                    join technology in _context.PreparationTechnologies on medicine.TechnologyId equals technology.TechnologyId into techJoin
                    from technology in techJoin.DefaultIfEmpty()
                    select new PreparationTechnologyInfo
                    {
                        MedicineId = medicine.MedicineId,
                        MedicineName = medicine.MedicineName,
                        MedicineTypeName = medicine.MedicineType.TypeName,
                        TechnologyId = technology != null ? technology.TechnologyId : (int?)null,
                        TechnologyCode = technology != null ? technology.TechnologyCode : "Не указана",
                        PreparationMethod = technology != null ? technology.PreparationMethod : medicine.Description ?? "Описание отсутствует",
                        PreparationTimeMinutes = technology != null ? technology.PreparationTimeMinutes : 0,
                        IsActive = technology != null ? technology.IsActive : false
                    };

        return query.ToList();
    }
    #endregion

    #region Запрос 11: Цены на лекарство и компоненты
    /// <summary>
    /// Получить сведения о ценах на указанное лекарство в готовом виде, 
    /// об объеме и ценах на все компоненты, требующиеся для этого лекарства
    /// </summary>
    public MedicinePriceInfo GetMedicinePriceInfo(int medicineId)
    {
        var medicine = _context.Medicines
            .Where(m => m.MedicineId == medicineId)
            .Select(m => new
            {
                m.MedicineId,
                m.MedicineName,
                m.SalePrice,
                m.ManufacturingCost,
                m.Unit.UnitName
            })
            .FirstOrDefault();

        if (medicine == null)
            return null;

        var componentsQuery = from composition in _context.MedicineCompositions
                              where composition.MedicineId == medicineId
                              join component in _context.Components on composition.ComponentId equals component.ComponentId
                              select new ComponentPriceInfo
                              {
                                  ComponentName = component.ComponentName,
                                  Quantity = composition.Quantity,
                                  UnitName = composition.Unit.UnitName,
                                  PurchasePrice = component.PurchasePrice,
                                  TotalCost = composition.Quantity * component.PurchasePrice
                              };

        var components = componentsQuery.ToList();
        decimal totalComponentsCost = components.Sum(c => c.TotalCost);

        return new MedicinePriceInfo
        {
            MedicineId = medicine.MedicineId,
            MedicineName = medicine.MedicineName,
            SalePrice = medicine.SalePrice,
            ManufacturingCost = medicine.ManufacturingCost ?? 0,
            UnitName = medicine.UnitName,
            Components = components,
            TotalComponentsCost = totalComponentsCost,
            ProfitMargin = medicine.SalePrice - totalComponentsCost
        };
    }
    #endregion

    #region Запрос 12: Наиболее активные клиенты
    /// <summary>
    /// Получить сведения о наиболее часто делающих заказы клиентах 
    /// на медикаменты определенного типа, на конкретные медикаменты
    /// </summary>
    public List<ActiveCustomerInfo> GetMostActiveCustomers(
        int? medicineTypeId = null,
        int? medicineId = null,
        int topCount = 10)
    {
        var query = from order in _context.Orders
                    join patient in _context.Patients on order.PatientId equals patient.PatientId
                    join medicine in _context.Medicines on order.MedicineId equals medicine.MedicineId
                    where medicineId == null || medicine.MedicineId == medicineId
                    where medicineTypeId == null || medicine.MedicineTypeId == medicineTypeId
                    group order by new 
                    { 
                        patient.PatientId, 
                        patient.LastName,
                        patient.FirstName,
                        patient.Patronymic,
                        patient.Phone
                    } into g
                    orderby g.Count() descending, g.Sum(x => x.TotalPrice) descending
                    select new ActiveCustomerInfo
                    {
                        PatientId = g.Key.PatientId,
                        FullName = $"{g.Key.LastName} {g.Key.FirstName} {g.Key.Patronymic}".Trim(),
                        Phone = g.Key.Phone,
                        OrdersCount = g.Count(),
                        TotalSpent = g.Sum(x => x.TotalPrice),
                        AverageOrderValue = g.Average(x => x.TotalPrice)
                    };

        return query.Take(topCount).ToList();
    }
    #endregion

    #region Запрос 13: Сведения о конкретном лекарстве
    /// <summary>
    /// Получить сведения о конкретном лекарстве (его тип, способ приготовления, 
    /// названия всех компонент, цены, его количество на складе)
    /// </summary>
    public DetailedMedicineInfo GetDetailedMedicineInfo(int medicineId)
    {
        var medicine = _context.Medicines
            .Where(m => m.MedicineId == medicineId)
            .Select(m => new
            {
                m.MedicineId,
                m.MedicineName,
                m.MedicineTypeId,
                MedicineTypeName = m.MedicineType.TypeName,
                m.CategoryId,
                CategoryName = m.Category != null ? m.Category.CategoryName : null,
                m.TechnologyId,
                PreparationMethod = m.Technology != null ? m.Technology.PreparationMethod : m.Description,
                m.CriticalNorm,
                CurrentStock = m.CurrentStock ?? 0,
                m.UnitId,
                UnitName = m.Unit.UnitName,
                m.ShelfLifeDays,
                m.ManufacturingCost,
                m.SalePrice,
                m.RequiresPrescription,
                m.IsReadyMade,
                m.Description
            })
            .FirstOrDefault();

        if (medicine == null)
            return null;

        var componentsQuery = from composition in _context.MedicineCompositions
                              where composition.MedicineId == medicineId
                              join component in _context.Components on composition.ComponentId equals component.ComponentId
                              select new MedicineComponentInfo
                              {
                                  ComponentName = component.ComponentName,
                                  Quantity = composition.Quantity,
                                  UnitName = composition.Unit.UnitName,
                                  PurchasePrice = component.PurchasePrice,
                                  CurrentStock = component.CurrentStock ?? 0
                              };

        return new DetailedMedicineInfo
        {
            MedicineId = medicine.MedicineId,
            MedicineName = medicine.MedicineName,
            MedicineTypeName = medicine.MedicineTypeName,
            CategoryName = medicine.CategoryName,
            PreparationMethod = medicine.PreparationMethod,
            CriticalNorm = medicine.CriticalNorm,
            CurrentStock = medicine.CurrentStock,
            UnitName = medicine.UnitName,
            ShelfLifeDays = medicine.ShelfLifeDays,
            ManufacturingCost = medicine.ManufacturingCost ?? 0,
            SalePrice = medicine.SalePrice,
            RequiresPrescription = medicine.RequiresPrescription ?? false,
            IsReadyMade = medicine.IsReadyMade ?? false,
            Description = medicine.Description,
            Components = componentsQuery.ToList()
        };
    }
    #endregion

    #region Дополнительные запросы по заданию

    /// <summary>
    /// Вывести список лекарств с критической нормой, у которых вышел срок хранения
    /// </summary>
    public List<ExpiredMedicineInfo> GetExpiredMedicines()
    {
        var query = from inventoryDetail in _context.InventoryDetails
                    where inventoryDetail.IsExpired == true
                    join inventory in _context.InventoryChecks on inventoryDetail.InventoryId equals inventory.InventoryId
                    select new ExpiredMedicineInfo
                    {
                        ItemType = inventoryDetail.ItemType,
                        ItemId = inventoryDetail.ItemId,
                        ExpirationDate = inventoryDetail.ExpirationDate,
                        ExpectedQuantity = inventoryDetail.ExpectedQuantity,
                        ActualQuantity = inventoryDetail.ActualQuantity,
                        CheckDate = inventory.CheckDate,
                        Notes = inventoryDetail.Notes
                    };

        return query.ToList();
    }

    /// <summary>
    /// Вывести недостачу по результатам инвентаризации
    /// </summary>
    public List<ShortageInfo> GetShortagesFromInventory()
    {
        var query = from inventoryDetail in _context.InventoryDetails
                    where inventoryDetail.Difference < 0
                    join inventory in _context.InventoryChecks on inventoryDetail.InventoryId equals inventory.InventoryId
                    select new ShortageInfo
                    {
                        ItemType = inventoryDetail.ItemType,
                        ItemId = inventoryDetail.ItemId,
                        ExpectedQuantity = inventoryDetail.ExpectedQuantity,
                        ActualQuantity = inventoryDetail.ActualQuantity,
                        ShortageAmount = Math.Abs((decimal)inventoryDetail.Difference),
                        UnitName = inventoryDetail.Unit.UnitName,
                        CheckDate = inventory.CheckDate,
                        Notes = inventoryDetail.Notes
                    };

        return query.ToList();
    }

    /// <summary>
    /// Выдать пациенту название лекарства и способ приготовления согласно рецепту
    /// </summary>
    public PrescriptionInfo GetPrescriptionInfo(int prescriptionId)
    {
        var prescription = _context.Prescriptions
            .Where(p => p.PrescriptionId == prescriptionId)
            .Select(p => new
            {
                p.PrescriptionId,
                p.PrescriptionNumber,
                PatientName = $"{p.Patient.LastName} {p.Patient.FirstName} {p.Patient.Patronymic}",
                DoctorName = $"{p.Doctor.LastName} {p.Doctor.FirstName} {p.Doctor.Patronymic}",
                p.IssueDate,
                p.ValidUntil,
                p.Diagnosis,
                MedicineName = p.Medicine != null ? p.Medicine.MedicineName : null,
                p.Quantity,
                p.Dosage,
                p.UsageInstructions,
                PreparationMethod = p.Medicine != null && p.Medicine.Technology != null 
                    ? p.Medicine.Technology.PreparationMethod 
                    : (p.Medicine != null ? p.Medicine.Description : null),
                p.IsFilled,
                p.FilledDate
            })
            .FirstOrDefault();

        if (prescription == null)
            return null;

        return new PrescriptionInfo
        {
            PrescriptionId = prescription.PrescriptionId,
            PrescriptionNumber = prescription.PrescriptionNumber,
            PatientName = prescription.PatientName,
            DoctorName = prescription.DoctorName,
            IssueDate = prescription.IssueDate,
            ValidUntil = prescription.ValidUntil,
            Diagnosis = prescription.Diagnosis,
            MedicineName = prescription.MedicineName,
            Quantity = prescription.Quantity,
            Dosage = prescription.Dosage,
            UsageInstructions = prescription.UsageInstructions,
            PreparationMethod = prescription.PreparationMethod,
            IsFilled = prescription.IsFilled ?? false,
            FilledDate = prescription.FilledDate
        };
    }
    #endregion
}

#region DTO классы для результатов запросов

public class PatientInfo
{
    public int PatientId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string OrderNumber { get; set; }
    public string MedicineName { get; set; }
    public DateTime RequiredDate { get; set; }
    public int DaysOverdue { get; set; }
}

public class PatientWaitingInfo
{
    public int PatientId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string OrderNumber { get; set; }
    public string MedicineName { get; set; }
    public string CategoryName { get; set; }
    public DateTime OrderDate { get; set; }
    public string MissingComponentsNote { get; set; }
}

public class MedicineUsageInfo
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; }
    public decimal TotalQuantity { get; set; }
    public int OrdersCount { get; set; }
    public int? CategoryId { get; set; }
}

public class ComponentUsageInfo
{
    public int ComponentId { get; set; }
    public string ComponentName { get; set; }
    public string UnitName { get; set; }
    public decimal TotalQuantity { get; set; }
    public int OrdersCount { get; set; }
}

public class PatientOrderInfo
{
    public int PatientId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string MedicineName { get; set; }
    public string MedicineTypeName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CriticalNormMedicineInfo
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; }
    public string MedicineTypeName { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal CriticalNorm { get; set; }
    public decimal ShortageAmount { get; set; }
    public bool IsOutOfStock { get; set; }
    public string UnitName { get; set; }
}

public class MinimumStockMedicineInfo
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; }
    public string CategoryName { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal CriticalNorm { get; set; }
    public string UnitName { get; set; }
    public decimal SalePrice { get; set; }
}

public class OrderInProductionInfo
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; }
    public string PatientName { get; set; }
    public string MedicineName { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ReadyDate { get; set; }
    public decimal Quantity { get; set; }
    public string ProductionEmployeeName { get; set; }
    public bool IsAllComponentsAvailable { get; set; }
    public string Notes { get; set; }
}

public class RequiredComponentsInfo
{
    public string OrderNumber { get; set; }
    public string MedicineName { get; set; }
    public string ComponentName { get; set; }
    public decimal RequiredQuantity { get; set; }
    public string UnitName { get; set; }
    public decimal CurrentStock { get; set; }
    public bool IsAvailable { get; set; }
}

public class PreparationTechnologyInfo
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; }
    public string MedicineTypeName { get; set; }
    public int? TechnologyId { get; set; }
    public string TechnologyCode { get; set; }
    public string PreparationMethod { get; set; }
    public int PreparationTimeMinutes { get; set; }
    public bool? IsActive { get; set; }
}

public class ComponentPriceInfo
{
    public string ComponentName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitName { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal TotalCost { get; set; }
}

public class MedicinePriceInfo
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; }
    public decimal SalePrice { get; set; }
    public decimal ManufacturingCost { get; set; }
    public string UnitName { get; set; }
    public List<ComponentPriceInfo> Components { get; set; }
    public decimal TotalComponentsCost { get; set; }
    public decimal ProfitMargin { get; set; }
}

public class ActiveCustomerInfo
{
    public int PatientId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public int OrdersCount { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class MedicineComponentInfo
{
    public string ComponentName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitName { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal CurrentStock { get; set; }
}

public class DetailedMedicineInfo
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; }
    public string MedicineTypeName { get; set; }
    public string CategoryName { get; set; }
    public string PreparationMethod { get; set; }
    public decimal CriticalNorm { get; set; }
    public decimal CurrentStock { get; set; }
    public string UnitName { get; set; }
    public int? ShelfLifeDays { get; set; }
    public decimal ManufacturingCost { get; set; }
    public decimal SalePrice { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsReadyMade { get; set; }
    public string Description { get; set; }
    public List<MedicineComponentInfo> Components { get; set; }
}

public class ExpiredMedicineInfo
{
    public string ItemType { get; set; }
    public int ItemId { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public DateTime CheckDate { get; set; }
    public string Notes { get; set; }
}

public class ShortageInfo
{
    public string ItemType { get; set; }
    public int ItemId { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal ShortageAmount { get; set; }
    public string UnitName { get; set; }
    public DateTime CheckDate { get; set; }
    public string Notes { get; set; }
}

public class PrescriptionInfo
{
    public int PrescriptionId { get; set; }
    public string PrescriptionNumber { get; set; }
    public string PatientName { get; set; }
    public string DoctorName { get; set; }
    public DateTime IssueDate { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string Diagnosis { get; set; }
    public string MedicineName { get; set; }
    public decimal Quantity { get; set; }
    public string Dosage { get; set; }
    public string UsageInstructions { get; set; }
    public string PreparationMethod { get; set; }
    public bool IsFilled { get; set; }
    public DateTime? FilledDate { get; set; }
}

#endregion
