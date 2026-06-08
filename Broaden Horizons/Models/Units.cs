namespace BroadenHorizons
{
    public class Unit
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public UnitTypeEnum Type { get; set; }
        public int Planet { get; set; }
        public int Region { get; set; }
        public UnitStatus Status { get; set; }
    }

    public class UnitType
    {
        public string Name { get; set; }
		public UnitTypeEnum Type { get; set; }
        public int FoodCost { get; set; }
        public int MatCost { get; set; }
        public int PopCost { get; set; }
        public int FoodMaint { get; set; }
        public int MatMaint { get; set; }
        public int ExtraFoodProd { get; set; } = 0;
        public int ExtraMatProd { get; set; } = 0;
        public int ExtraSciProd { get; set; } = 0;
        public int TextureId { get; set; }
        public int RecruitTurns { get; set; }
        public int RequiredTech { get; set; }
    }

    public enum UnitTypeEnum
    {
        Explorers = 0,
        Farmers = 1,
        Miners = 2,
        Scientists = 3,
        Builders = 4,
        Harvesters = 5,
        Fishermen = 6
    }
	
	public enum UnitStatus
	{
	    InImprovement = -1,
	    Idle = 0,
	    Busy = 1
	}
}