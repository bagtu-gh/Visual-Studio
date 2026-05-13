namespace BroadenHorizons
{
    public class Unit
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int TypeIndex { get; set; }
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
        Explorer = 0,
        Farmer = 1,
        Miner = 2,
        Scientist = 3,
        Builder = 4,
        Harvester = 5,
        Fisher = 6,
        Colonist = 7
    }
	
	public enum UnitStatus
	{
	    Occupied = -1,
	    Idle = 0,
	    Busy = 1
	}

    public enum UnitActionType
    {
        None = 0,
        Building = 1,
        Recruiting = 2,
        MovingOrExploring = 3
    }

}