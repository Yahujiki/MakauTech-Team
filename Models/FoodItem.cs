namespace MakauTech.Models
{
    public abstract class FoodItem : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public int PointValue { get; private set; }
        public bool IsHazard { get; protected set; } = false;

        protected FoodItem(int pointValue) { PointValue = pointValue; }

        // Abstract — every food type must implement
        public abstract string GetFoodDescription();
        public abstract double GetSpawnChance(); // 0.0 to 1.0

        // Virtual — can override
        public virtual string GetCatchMessage() => $"+{PointValue} pts! Delicious!";

        // Polymorphism from BaseEntity
        public override string GetDisplayInfo() => $"{Emoji} {Name} — {GetFoodDescription()} ({PointValue}pts)";
    }

    // Inheritance chain:
    public class CommonFood : FoodItem
    {
        public CommonFood(string name, string emoji) : base(1)
        { Name = name; Emoji = emoji; }
        public override string GetFoodDescription() => "A popular Sibu street food ";
        public override double GetSpawnChance() => 0.02;
    }

    public class UncommonFood : FoodItem
    {
        public UncommonFood(string name, string emoji) : base(2)
        { Name = name; Emoji = emoji; }
        public override string GetFoodDescription() => "A special Sibu snack nice catch!";
        public override double GetSpawnChance() => 0.07;
        public override string GetCatchMessage() => $"+{PointValue} pts! Tasty!";
    }

    public class RareFood : FoodItem
    {
        public RareFood(string name, string emoji) : base(3)
        { Name = name; Emoji = emoji; }
        public override string GetFoodDescription() => "Regional favourite — harder to grab!";
        public override double GetSpawnChance() => 0.09;
        public override string GetCatchMessage() => $"+{PointValue} pts! Great catch!";
    }

    public class PremiumFood : FoodItem
    {
        public PremiumFood(string name, string emoji) : base(4)
        { Name = name; Emoji = emoji; }
        public override string GetFoodDescription() => "Premium Sibu flavour — big points!";
        public override double GetSpawnChance() => 0.08;
        public override string GetCatchMessage() => $"+{PointValue} pts! Outstanding!";
    }

    public class GoldenTrophy : FoodItem
    {
        public GoldenTrophy() : base(5)
        { Name = "Golden Trophy"; Emoji = "🏆"; }
        public override string GetFoodDescription() => "Ultra rare golden trophy — massive bonus!";
        public override double GetSpawnChance() => 0.14;
        public override string GetCatchMessage() => "+5 pts! LEGENDARY!";
    }

    public class HazardFood : FoodItem
    {
        public HazardFood() : base(0)
        { Name = "Rotten Food"; Emoji = "💀"; IsHazard = true; }
        public override string GetFoodDescription() => "Avoid this! Costs you a life.";
        public override double GetSpawnChance() => 0.05;
        public override string GetCatchMessage() => "💀 Ouch! Lost a life!";
    }
}
