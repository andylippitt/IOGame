﻿namespace Game.Robots
{
    using Game.Robots.Behaviors;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;

    public class ContextTurret : ContextRobot
    {
        protected readonly NavigateToPoint Navigation;
        private long CustomDataTime = 0;

        public ContextTurret(Vector2 target)
        {
            Behaviors.Add(Navigation = new NavigateToPoint(this) { BehaviorWeight = 0.00f });
            Behaviors.Add(new Efficiency(this) { BehaviorWeight = 0.1f });
            Behaviors.Add(new Dodge(this) { LookAheadMS = 250, BehaviorWeight = 4 });
            Behaviors.Add(new Dodge(this) { LookAheadMS = 500, BehaviorWeight = 2 });
            Behaviors.Add(new Dodge(this) { LookAheadMS = 1000, BehaviorWeight = 1 });
            Behaviors.Add(new StayInBounds(this) { BehaviorWeight = 1f });

            Navigation.TargetPoint = target;
            Steps = 16;
        }

        protected async override Task AliveAsync()
        {
            if (CanShoot)
            {
                var closest = SensorFleets.Others
                    .OrderBy(f => Vector2.Distance(this.Position, f.Center))
                    .Where(f => f.Name != this.Name)
                    .FirstOrDefault();

                if (closest != null)
                {
                    ShootAt(closest.Center);
                }
            }

            if (CanBoost && (SensorFleets.MyFleet?.Ships.Count ?? 0) > 8 )
                Boost();

            
            /*if (CustomDataTime + 200 < GameTime)
            {
                CustomDataTime = GameTime;
                CustomData = JsonConvert.SerializeObject(new
                {
                    spots = Behaviors.OfType<Dodge>().Where(d => d.ConsideredPoints != null).SelectMany(d => d.ConsideredPoints)
                });
            }*/

            await base.AliveAsync();
        }
    }
}
