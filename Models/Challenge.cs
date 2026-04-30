// Challenge.cs
// OOP: Abstract class + Inheritance + Polymorphism
// Alan Ting verbal: "Challenge is abstract — cannot instantiate directly.
// Each challenge type overrides GetChallengeDescription() differently."

namespace MakauTech.Models
{
    // ABSTRACT CLASS — cannot do new Challenge()
    public abstract class Challenge : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string PlaceName { get; set; } = string.Empty;
        public int PointsReward { get; set; } = 10;
        public bool IsCompleted { get; set; } = false;

        // ABSTRACT METHOD — every child must implement differently
        public abstract string GetChallengeDescription();

        // VIRTUAL METHOD — child can override or use default
        public virtual string GetRewardText()
        {
            return $"+{PointsReward} points";
        }

        // POLYMORPHISM: GetDisplayInfo overridden from BaseEntity
        public override string GetDisplayInfo()
        {
            return $"{Title} — {PlaceName} ({GetRewardText()})";
        }
    }

    // INHERITANCE: VisitChallenge inherits Challenge
    // POLYMORPHISM: Different GetChallengeDescription()
    public class VisitChallenge : Challenge
    {
        public VisitChallenge(string placeName)
        {
            Title = "Visit Challenge";
            PlaceName = placeName;
            PointsReward = 10;
        }

        public override string GetChallengeDescription()
        {
            return $"Visit {PlaceName} and check in to earn {PointsReward} bonus points!";
        }

        public override string GetRewardText()
        {
            return $"+{PointsReward} bonus pts for visiting!";
        }
    }

    // INHERITANCE: ReviewChallenge inherits Challenge
    public class ReviewChallenge : Challenge
    {
        public ReviewChallenge(string placeName)
        {
            Title = "Review Challenge";
            PlaceName = placeName;
            PointsReward = 15;
        }

        public override string GetChallengeDescription()
        {
            return $"Visit {PlaceName} and write a detailed review to earn {PointsReward} bonus points!";
        }
    }

    // INHERITANCE: ExplorerChallenge inherits Challenge
    public class ExplorerChallenge : Challenge
    {
        public int PlacesRequired { get; set; } = 3;

        public ExplorerChallenge(string placeName, int placesRequired = 3)
        {
            Title = "Explorer Challenge";
            PlaceName = placeName;
            PointsReward = 20;
            PlacesRequired = placesRequired;
        }

        public override string GetChallengeDescription()
        {
            return $"Visit {PlacesRequired} places including {PlaceName} to unlock the Explorer badge and earn {PointsReward} bonus points!";
        }

        public override string GetRewardText()
        {
            return $"+{PointsReward} pts + Explorer Badge!";
        }
    }
}