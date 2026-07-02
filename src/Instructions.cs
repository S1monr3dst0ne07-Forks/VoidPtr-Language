namespace Interpreter;

public enum AddressMode : byte
{
    Direct, //number
    Indirect, //[number]
    Const  //$number
}

public enum Operator : byte
{
    None,
    And,
    Or,
    Xor,
    Not,
    Cmp,
    Jmp,
    ShiftRight,
    ShiftLeft,
    Assign,
    MemSet
}

public struct InstrValue(AddressMode mode,int val)
{
    public AddressMode mode = mode;
    public int value = val;

    public override string ToString()
    {
        return $"[{mode}:{value}]";
    }
}

public struct Instruction(InstrValue val1, InstrValue val2, Operator op)
{
    public InstrValue val1 = val1;
    public InstrValue val2 = val2;
    public InstrValue val3;
    public Operator type = op;

    public override string ToString()
    {
        return $"{type} {val1},{val2},{val3}";
    }
}