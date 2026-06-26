
using Errors;

namespace Interpreter;

public class Engine
{
    
    private byte[] memory;
    private int pc = 0;
    private List<Instruction> code;

    public Engine(List<Instruction> code)
    {
        this.code = code;
        memory = new byte[4096];
        
    }

    private byte GetValue(in InstrValue val)
    {
        return val.mode switch
        {
            AddressMode.Direct => memory[val.value],
            AddressMode.Indirect => memory[memory[val.value]],
            AddressMode.Const => (byte)val.value,
            _ => 0,
        };
    }

    

    //returns actual memory address
    private int MemAccess(in InstrValue val)
    {
        switch(val.mode)
        {
            case AddressMode.Direct:
                return val.value;
            case AddressMode.Indirect:
                return memory[val.value];
            default:
                return -1;
        }
    }

    private void Assign(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val1)] = (byte)instruction.val2.value;
            return;
        }
        memory[MemAccess(instruction.val2)] = memory[MemAccess(instruction.val1)];
    }

    private void And(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] & (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] & memory[MemAccess(instruction.val2)]);
    }

    private void Or(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] | (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] | memory[MemAccess(instruction.val2)]);
    }

    private void Xor(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] ^ (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] ^ memory[MemAccess(instruction.val2)]);
    }

    private void ShiftLeft(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] << (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] << memory[MemAccess(instruction.val2)]);
    }
    
    private void ShiftRight(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] >> (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] >> memory[MemAccess(instruction.val2)]);
    }

    private void Not(in Instruction instruction)
    {
        memory[MemAccess(instruction.val2)] = (byte)~memory[MemAccess(instruction.val1)];
    }

    private void Cmp(in Instruction instruction)
    {
        if(memory[MemAccess(instruction.val1)] != 0)
        {
            pc++;
        }
    }

    private void Jmp(in Instruction instruction)
    {
        pc = instruction.val1.value;
    }

    private void SysCall()
    {
        switch(memory[0])
        {
            case 1: //print num
                Console.WriteLine(memory[memory[1]]);
            break;

            case 2: //print char
                Console.WriteLine((char)memory[memory[1]]);
            break;

            case 3: //Alloc mem
                byte[] nArr = new byte[memory.Length+memory[memory[1]]];
                memory.CopyTo(nArr,0);
                memory = nArr;
            break;

            case 4: //Free mem
                byte[] freedArr = new byte[memory.Length - memory[memory[1]]];
                Array.Copy(memory,freedArr,Math.Min(memory.Length,freedArr.Length));
            break;

        }
    }

    public void Run()
    {
        try
        {
            while(pc<code.Count)
            {
                switch(code[pc].type)
                {
                    case Operator.Assign:
                        Assign(code[pc]);
                    break;

                    case Operator.And:
                        And(code[pc]);
                    break;

                    case Operator.Or:
                        Or(code[pc]);
                    break;

                    case Operator.Xor:
                        Xor(code[pc]);
                    break;

                    case Operator.Not:
                        Not(code[pc]);
                    break;

                    case Operator.Cmp:
                        Cmp(code[pc]);
                    break;

                    case Operator.ShiftLeft:
                        ShiftLeft(code[pc]);
                    break;

                    case Operator.ShiftRight:
                        ShiftRight(code[pc]);
                    break;

                    case Operator.Jmp:
                        Jmp(code[pc]);
                    break;

                }
                if(memory[0] != 0)
                {
                    SysCall();
                    memory[0] = 0;
                }
                pc++;
            }
        }
        catch(IndexOutOfRangeException)
        {
            ErrorHandler.Throw("Segmentation fault :3");
        }
        
    }


}