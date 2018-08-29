// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace Game.Engine.Networking.FlatBuffers
{

using global::System;
using global::FlatBuffers;

public struct Quantum : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static Quantum GetRootAsQuantum(ByteBuffer _bb) { return GetRootAsQuantum(_bb, new Quantum()); }
  public static Quantum GetRootAsQuantum(ByteBuffer _bb, Quantum obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
  public Quantum __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public AllMessages MessageType { get { int o = __p.__offset(4); return o != 0 ? (AllMessages)__p.bb.Get(o + __p.bb_pos) : AllMessages.NONE; } }
  public TTable? Message<TTable>() where TTable : struct, IFlatbufferObject { int o = __p.__offset(6); return o != 0 ? (TTable?)__p.__union<TTable>(o) : null; }

  public static Offset<Quantum> CreateQuantum(FlatBufferBuilder builder,
      AllMessages message_type = AllMessages.NONE,
      int messageOffset = 0) {
    builder.StartObject(2);
    Quantum.AddMessage(builder, messageOffset);
    Quantum.AddMessageType(builder, message_type);
    return Quantum.EndQuantum(builder);
  }

  public static void StartQuantum(FlatBufferBuilder builder) { builder.StartObject(2); }
  public static void AddMessageType(FlatBufferBuilder builder, AllMessages messageType) { builder.AddByte(0, (byte)messageType, 0); }
  public static void AddMessage(FlatBufferBuilder builder, int messageOffset) { builder.AddOffset(1, messageOffset, 0); }
  public static Offset<Quantum> EndQuantum(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<Quantum>(o);
  }
  public static void FinishQuantumBuffer(FlatBufferBuilder builder, Offset<Quantum> offset) { builder.Finish(offset.Value); }
  public static void FinishSizePrefixedQuantumBuffer(FlatBufferBuilder builder, Offset<Quantum> offset) { builder.FinishSizePrefixed(offset.Value); }
};


}
