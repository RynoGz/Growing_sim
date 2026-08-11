using System;

namespace Growveld.Economy
{
    [Serializable]
    public sealed class DailyUtilityBill
    {
        public DailyUtilityBill(int day, float electricityCost, float waterCost, float electricityKwh, float waterLitres)
        {
            Day = day;
            ElectricityCost = electricityCost;
            WaterCost = waterCost;
            ElectricityKilowattHours = electricityKwh;
            WaterLitres = waterLitres;
        }

        public int Day { get; }
        public float ElectricityCost { get; }
        public float WaterCost { get; }
        public float TotalCost => ElectricityCost + WaterCost;
        public float ElectricityKilowattHours { get; }
        public float WaterLitres { get; }
    }
}
