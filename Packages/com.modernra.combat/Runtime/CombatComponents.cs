using Unity.Entities;

namespace ModernRA.Combat
{
    public enum ArmorClass : byte { Soft, Light, Medium, Heavy, Fortified, Naval, Air }
    public enum DamageClass : byte { Kinetic, Explosive, ShapedCharge, Energy, Electronic, Neural }

    public struct ArmorState : IComponentData { public ArmorClass Class; public ushort Front; public ushort Side; public ushort Rear; }
    public struct WeaponState : IComponentData { public int WeaponIndex; public ushort CooldownTicks; public ushort Ammo; }
    public struct DamageEvent : IBufferElementData
    {
        public Entity Source;
        public int RawDamage;
        public DamageClass DamageClass;
        public short DirectionCentidegrees;
    }
}
