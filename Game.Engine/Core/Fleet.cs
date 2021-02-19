﻿namespace Game.Engine.Core
{
    using Game.API.Common;
    using Game.Engine.Core.Steering;
    using Game.Engine.Core.Weapons;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    public class Fleet : ActorGroup
    {
        public virtual float ShotCooldownTimeM { get => World.Hook.ShotCooldownTimeM; }
        public virtual float ShotCooldownTimeB { get => World.Hook.ShotCooldownTimeB; }
        public virtual int ShotCooldownTimeShark { get => World.Hook.ShotCooldownTimeShark; }
        public virtual float ShotThrustM { get => World.Hook.ShotThrustM; }
        public virtual float ShotThrustB { get => World.Hook.ShotThrustB; }
        public virtual float BaseThrustM { get => World.Hook.BaseThrustM; }
        public virtual float BaseThrustB { get => World.Hook.BaseThrustB; }
        public virtual float[] BaseThrust { get => World.Hook.BaseThrust; }
        public virtual float BaseThrustConverter { get => World.Hook.BaseThrustConverter; }
        public virtual float BoostThrust { get => World.Hook.BoostThrust; }

        public virtual int SpawnShipCount { get => World.Hook.SpawnShipCount; }

        public virtual bool BossMode { get => World.Hook.BossMode && World.Hook.BossModeSprites.Contains(this.Owner.ShipSprite); }

        public Player Owner { get; set; }

        public bool BoostRequested { get; set; }
        public bool ShootRequested { get; set; }

        public long ShootCooldownTimeStart { get; set; } = 0;
        public long ShootCooldownTime { get; set; } = 0;
        public float ShootCooldownStatus { get; set; } = 0;
        public long BoostCooldownTimeStart { get; set; } = 0;
        public long BoostCooldownTime { get; set; } = 0;
        public float BoostCooldownStatus { get; set; } = 0;
        public long BoostUntil { get; set; } = 0;
        public long BoostUntil2 { get; set; } = 0;
        public float BoostAngle { get; set; } = 0;

        public Vector2 AimTarget { get; set; }
        public Vector2 AimTarget2 { get; set; }

        public List<Ship> Ships { get; set; } = new List<Ship>();
        public List<Ship> NewShips { get; set; } = new List<Ship>();

        public List<ShipWeaponBullet> NewBullets { get; set; } = new List<ShipWeaponBullet>();

        public IFleetWeapon BaseWeapon { get; set; }
        public Stack<IFleetWeapon> WeaponStack { get; set; } = new Stack<IFleetWeapon>();

        public Vector2 FleetCenter = Vector2.Zero;
        public Vector2 FleetMomentum = Vector2.Zero;

        public float Burden { get; set; } = 0f;
        public bool Shark { get; set; } = false;
        public bool LastTouchedLeft { get; set; } = false;
        public bool FiringWeapon { get; private set; } = false;
        
        public uint DangerSince { get; set; } = 0;
        public uint DangerDecayCounter { get; set; } = 0;
        
        public uint ShipGainCounter { get; set; } = 0;

        public Vector2? SpawnLocation { get; set; } = null;
        public int ShipSize { get; set; } = 10;

        public Queue<long> EarnedShips = new Queue<long>();

        [Flags]
        public enum ShipModeEnum
        {
            none = 0,
            boost = 1,
            invulnerable = 2,
            defense_upgrade = 4,
            offense_upgrade = 8,
            shield = 16
        }

        public Sprites BulletSprite
        {
            get
            {
                switch (Owner.ShipSprite)
                {
                    case Sprites.ship_cyan: return Sprites.bullet_cyan;
                    case Sprites.ship_blue: return Sprites.bullet_blue;
                    case Sprites.ship_green: return Sprites.bullet_green;
                    case Sprites.ship_orange: return Sprites.bullet_orange;
                    case Sprites.ship_pink: return Sprites.bullet_pink;
                    case Sprites.ship_red: return Sprites.bullet_red;
                    case Sprites.ship_yellow: return Sprites.bullet_yellow;
                    case Sprites.ship_zed: return Sprites.bullet_red;
                    default: return Sprites.bullet;
                }
            }
        }

        private void Die(Player player)
        {
            World.Scoring.FleetDied(player, Owner, this);

            this.Owner.Die(player);

            PendingDestruction = true;
            NewShips.Clear();
        }

        public override void Destroy()
        {
            foreach (var ship in Ships.ToList())
                ship.Destroy();

            base.Destroy();
        }

        public void ShipDeath(Player player, Ship ship, ShipWeaponBullet bullet)
        {
            this.Owner.LastShipDeathKiller = bullet?.OwnedByFleet?.Owner;
            this.Owner.LastShipDeathTime = World.Time;

            if (!Ships.Where(s => !s.PendingDestruction).Any())
                Die(player);
        }

        public void KilledShip(Ship killedShip)
        {
            uint threshold;
            if (Ships.Count <= 30) {
                threshold = 1;
            } else if (Ships.Count <= 60) {
                threshold = 2;
            } else if (Ships.Count <= 80) {
                threshold = 3;
            } else if (Ships.Count <= 90) {
                threshold = 4;
            } else if (Ships.Count <= 99) {
                threshold = 5;
            } else {
                threshold = 0;
            }
            this.ShipGainCounter += 1;
            if (threshold <= this.ShipGainCounter)
                if ((Ships?.Any() ?? false)
                  && Ships.Count <= 99
                  && !(this.Owner.LastShipDeathKiller == killedShip?.Fleet?.Owner
                  && World.Time - this.Owner.LastShipDeathTime <= World.Hook.MutualDestructionCooldown))
                {
                    EarnedShips.Enqueue(World.Time + World.Hook.EarnedShipDelay);
                    this.ShipGainCounter = 0;
                }
        }

        public void AddShip(Sprites? sprite = null)
        {
            if (!this.Owner.IsAlive || this.PendingDestruction)
                return;

            var random = new Random();

            var offset = new Vector2
            (
                random.Next(-World.Hook.ShipAddRadius, World.Hook.ShipAddRadius),
                random.Next(-World.Hook.ShipAddRadius, World.Hook.ShipAddRadius)
            );

            var ship = new Ship()
            {
                Fleet = this,
                Sprite = this.Owner.ShipSprite,
                Color = this.Owner.Color,
                Size = ShipSize
            };

            if (this.Ships.Any())
            {
                var position = Vector2.Zero;
                var momentum = Vector2.Zero;
                var angle = 0f;
                var count = 0;

                foreach (var existingShip in this.Ships)
                {
                    position += existingShip.Position;
                    momentum += existingShip.Momentum;
                    angle += existingShip.Angle;
                    count++;
                }

                ship.Position = position / count + offset;
                ship.Momentum = momentum / count * World.Hook.ShipAddMomentumMultiplier;
                ship.Angle = angle / count;
            }
            else
            {
                ship.Position = FleetCenter + offset;
            }

            NewShips.Add(ship);
        }

        public override void Init(World world)
        {
            base.Init(world);
            this.GroupType = GroupTypes.Fleet;
            this.ZIndex = 100;

            this.BaseWeapon = new FleetWeaponGeneric<ShipWeaponBullet>();

            if (SpawnLocation != null)
                FleetCenter = SpawnLocation.Value;
            else
                FleetCenter = world.RandomSpawnPosition(this);

            for (int i = 0; i < SpawnShipCount; i++)
                this.AddShip();
        }

        public override void CreateDestroy()
        {
            if (FiringWeapon)
            {
                var weapon = this.WeaponStack.Any()
                    ? this.WeaponStack.Pop()
                    : this.BaseWeapon;

                weapon.FireFrom(this);
                FiringWeapon = false;
            }

            while (EarnedShips.Any() && EarnedShips.Peek() < World.Time)
            {
                AddShip();
                EarnedShips.Dequeue();
            }

            foreach (var ship in NewShips)
            {
                ship.Init(World);
                Ships.Add(ship);
            }
            NewShips.Clear();

            foreach (var bullet in NewBullets)
                bullet.Init(World);

            NewBullets.Clear();

            base.CreateDestroy();
        }

        public void Abandon()
        {
            foreach (var ship in Ships.ToList())
                this.AbandonShip(ship);
        }

        public void AbandonShip(Ship ship)
        {
            ship.Fleet = null;
            switch (Owner.ShipSprite)
            {
                case Sprites.ship_cyan: ship.Sprite = Sprites.ship_ab_cyan; break;
                case Sprites.ship_blue: ship.Sprite = Sprites.ship_ab_blue; break;
                case Sprites.ship_green: ship.Sprite = Sprites.ship_ab_green; break;
                case Sprites.ship_orange: ship.Sprite = Sprites.ship_ab_orange; break;
                case Sprites.ship_pink: ship.Sprite = Sprites.ship_ab_pink; break;
                case Sprites.ship_red: ship.Sprite = Sprites.ship_ab_red; break;
                case Sprites.ship_yellow: ship.Sprite = Sprites.ship_ab_yellow; break;
                case Sprites.ship_zed: ship.Sprite = Sprites.ship_ab_red; break;
                default: ship.Sprite = Sprites.ship_gray; break;
            }
            ship.Color = "gray";
            ship.Abandoned = true;
            ship.Momentum = ship.Momentum * World.Hook.AbandonMomentumMultiplier;
            ship.Group = null;
            ship.ThrustAmount = 0;
            ship.Mode = 0;
            ship.AbandonedByFleet = this;
            ship.AbandonedTime = World.Time;

            if (Ships.Contains(ship))
                Ships.Remove(ship);
        }


        public void PushStackWeapon(IFleetWeapon weapon)
        {
            WeaponStack.Push(weapon);
            if (WeaponStack.Count > World.Hook.FleetWeaponStackDepth)
                WeaponStack = new Stack<IFleetWeapon>(WeaponStack.TakeLast(World.Hook.FleetWeaponStackDepth));
        }

        public override void Think()
        {
            bool isShooting = ShootRequested && World.Time >= ShootCooldownTime;
            bool isBoosting = World.Time < BoostUntil;
            bool isBoosting2 = World.Time < BoostUntil2;
            bool isBoostInitial = false;
            bool isSleeping = AimTarget.X == 0 && AimTarget.Y == 0;

            float MinPointerDistance = (float)(World.Hook.MinPointerDistanceM * Ships.Count + World.Hook.MinPointerDistanceB);
            float MaxPointerDistance = (float)(World.Hook.MaxPointerDistanceM * Ships.Count + World.Hook.MaxPointerDistanceB);
            float c = (float)Math.Min(Math.Max(AimTarget.Length(), MinPointerDistance), MaxPointerDistance) / AimTarget.Length();
            if (!isSleeping) {
                AimTarget2 = new Vector2(AimTarget.X * c, AimTarget.Y * c);
            } else {
                double circlingAngle = (Math.Log10(Ships.Count) + 1) * 0.1;
                AimTarget2 = new Vector2(
                    (float)(AimTarget2.X * Math.Cos(circlingAngle) - AimTarget2.Y * Math.Sin(circlingAngle)),
                    (float)(AimTarget2.X * Math.Sin(circlingAngle) + AimTarget2.Y * Math.Cos(circlingAngle))
                );
            }

            if (World.Time > BoostCooldownTime && BoostRequested && Ships.Count > 1)
            {
                BoostCooldownTime = World.Time + (long)
                    (World.Hook.BoostCooldownTimeM * Ships.Count + World.Hook.BoostCooldownTimeB);
                BoostCooldownTimeStart = World.Time;

                BoostUntil = World.Time + World.Hook.BoostDuration;
                BoostUntil2 = World.Time + World.Hook.BoostDuration2;

                BoostAngle = MathF.Atan2(AimTarget.Y, AimTarget.X);
                isBoostInitial = true;
                var shipLoss = (int)MathF.Floor(Ships.Count / 2);
                var Sorter = Ships.OrderByDescending((ship) => Vector2.DistanceSquared(FleetCenter + AimTarget, ship.Position)).Take(shipLoss);
                foreach (Ship ship in Sorter)
                {
                    AbandonShip(ship);
                }
            }

            FleetCenter = FleetMath.FleetCenterNaive(this.Ships);
            FleetMomentum = FleetMath.FleetMomentum(this.Ships);

            Vector2 shipTargetVector = new Vector2();
            float angleMovement = 0f;
            float angle = MathF.Atan2(AimTarget.Y, AimTarget.X);
            float BoostM = (float)Math.Pow(Ships.Count, -0.205); // 4D Klein Manifold's magical formula

            foreach (var ship in Ships)
            {
                // todo: this dirties the ship body every cycle
                if (!isSleeping)
                    ship.Angle = angle;

                shipTargetVector = FleetCenter - ship.Position + AimTarget2;
    
                angleMovement = MathF.Atan2(shipTargetVector.Y, shipTargetVector.X);
                if (!float.IsNaN(angleMovement))
                    ship.AngleMovement =  angleMovement;

                Flocking.Flock(ship);

                float baseThrust = (BaseThrust[Ships.Count] * BaseThrustConverter);

                float boostf = (float)(BoostUntil - World.Time) / 1000;
                float boostf2 = (float)(BoostUntil2 - World.Time) / 1000;

                ship.ThrustAmount = isBoosting
                    ? baseThrust + (BoostThrust - baseThrust) * (float)Math.Pow(boostf2, 1.25f) /*(1 - (float)Math.Pow(2 * boostf2 - 1f, 2f))*/ * (1 - Burden) * BoostM 
                    : baseThrust * (1 - Burden);
                
                ship.BoostThrustAmount = isBoosting2
                    ? (float)Math.Pow(boostf2, 1.75f) * World.Hook.BoostThrust2 * (1 - Burden) * BoostM
                    : 0f;

                ship.Drag = isBoosting
                    ? World.Hook.DragBoost/* + (1f - World.Hook.DragBoost) * (float)Math.Pow(boostf2, 2.5f)*/
                    : World.Hook.Drag;

                ship.Mode = (byte)
                    (
                        (isBoosting ? ShipModeEnum.boost : ShipModeEnum.none)
                        | (WeaponStack.Any(w => w.IsOffense) ? ShipModeEnum.offense_upgrade : ShipModeEnum.none)
                        | (WeaponStack.Any(w => w.IsDefense) ? ShipModeEnum.defense_upgrade : ShipModeEnum.none)
                        | (!Owner.IsShielded && Owner.IsInvulnerable ? ShipModeEnum.invulnerable : ShipModeEnum.none)
                        | (Owner.IsShielded && ship.ShieldStrength > 0 ? ShipModeEnum.shield : ShipModeEnum.none)
                    );

                if (isBoostInitial)
                    //BoostAngle = angle;
                    if (ship.Momentum != Vector2.Zero)
                        ship.Momentum += Vector2.Normalize(FleetMomentum) * World.Hook.BoostSpeed * BoostM;

                if (ship.Momentum.Length() > (ship.ThrustAmount + ship.BoostThrustAmount) * World.Hook.MaxMomentumCoefficient) {
                    ship.Momentum = Vector2.Multiply(Vector2.Normalize(ship.Momentum), (ship.ThrustAmount + ship.BoostThrustAmount) * World.Hook.MaxMomentumCoefficient);
                }
                
                /*if (ship.Momentum.LengthSquared() != 0) {
                    ship.Momentum = Vector2.Multiply(Vector2.Normalize(ship.Momentum), (Single)Math.Round(ship.Momentum.Length()*200)/200);
                }*/
            }

            if (isShooting)
            {
                ShootCooldownTime = World.Time + (Shark ? ShotCooldownTimeShark : (int)(ShotCooldownTimeM * Ships.Count + ShotCooldownTimeB));
                ShootCooldownTimeStart = World.Time;

                FiringWeapon = true;
            }

            if (World.Time > BoostCooldownTime)
                BoostCooldownStatus = 1;
            else
                BoostCooldownStatus = (float)
                    (World.Time - BoostCooldownTimeStart) / (BoostCooldownTime - BoostCooldownTimeStart);

            if (World.Time > ShootCooldownTime)
                ShootCooldownStatus = 1;
            else
                ShootCooldownStatus = (float)
                    (World.Time - ShootCooldownTimeStart) / (ShootCooldownTime - ShootCooldownTimeStart);

            if (MathF.Abs(FleetCenter.X) > World.Hook.WorldSize &&
                FleetCenter.X < 0 != LastTouchedLeft &&
                World.Hook.Name == "Sharks and Minnows" &&
                !Owner.IsInvulnerable &&
                !Shark)
            {
                LastTouchedLeft = FleetCenter.X < 0;
                Owner.IsInvulnerable = true;
                Owner.SpawnTime = World.Time;
                Owner.Score++;
            }

            if (!Ships.Where(s => !s.PendingDestruction).Any()
                && !NewShips.Any())
                Die(null);
        }

    }
}