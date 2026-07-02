
using Errors;

namespace Interpreter;

public class Engine
{
    //This interpreter could be much more optimized by removing the branching and hardcoding addressing modes.
    //Even better: re-write this thing in C or write a compiler.

    private byte[] memory;
    private int pc = 0;
    private List<Instruction> code;

    public Engine(List<Instruction> code)
    {
        this.code = code;
        memory = new byte[4096]; 
        
    }
    
    //Returns resolved memory address based on address mode.
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

    // ->
    private void Assign(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val1)] = (byte)instruction.val2.value;
            return;
        }
        memory[MemAccess(instruction.val2)] = memory[MemAccess(instruction.val1)];
    }

    // &
    private void And(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] & (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] & memory[MemAccess(instruction.val2)]);
    }

    // |
    private void Or(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] | (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] | memory[MemAccess(instruction.val2)]);
    }

    // ^
    private void Xor(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] ^ (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] ^ memory[MemAccess(instruction.val2)]);
    }

    // <
    private void ShiftLeft(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] << (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] << memory[MemAccess(instruction.val2)]);
    }

    // >    
    private void ShiftRight(in Instruction instruction)
    {
        if(instruction.val2.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] >> (byte)instruction.val2.value);
            return;
        }
        memory[MemAccess(instruction.val3)] = (byte)(memory[MemAccess(instruction.val1)] >> memory[MemAccess(instruction.val2)]);
    }

    // !
    private void Not(in Instruction instruction)
    {
        if(instruction.val1.mode == AddressMode.Const)
        {
            memory[MemAccess(instruction.val2)] = (byte)~instruction.val1.value;
            return;
        }
        memory[MemAccess(instruction.val2)] = (byte)~memory[MemAccess(instruction.val1)];
    }

    // ?
    private void Cmp(in Instruction instruction)
    {
        if(memory[MemAccess(instruction.val1)] != 0)
        {
            pc++;
        }
    }

    // ' 
    private void Jmp(in Instruction instruction)
    {
        pc = instruction.val1.value;
    }

    private void SysCall()
    {
        switch(memory[0])
        {
            case 1: //Print num
                Console.Write(memory[memory[1]]);
            break;

            case 2: //Print char
                Console.Write((char)memory[memory[1]]);
            break;

            case 3: //Take console input
                memory[memory[1]] = (byte)Console.ReadKey().KeyChar;
            break;

            case 4: //Alloc mem
                byte[] nArr = new byte[memory.Length+memory[memory[1]]];
                memory.CopyTo(nArr,0);
                memory = nArr;
                GC.Collect();
            break;

            case 5: //Free mem
                byte[] freedArr = new byte[memory.Length - memory[memory[1]]];
                Array.Copy(memory,freedArr,Math.Min(memory.Length,freedArr.Length));
                GC.Collect();
            break;

            case 6: //Load 32 bit address, endianess depends on hardware. Good luck.
                Load32Sys();
            break;

            case 7: //Set 32 bit address
               Set32Sys();
            break;

            case 8: //Load programm counter
                LoadPCSys();
            break;

            case 9: //Load and set pc to passed value
                SetPCSys();
            break;

            case 10: //Load file
                LoadFSys();
            break;

        }
    }

    private void LoadPCSys()
    {
        //Loads the programm counter into space where 1 points to.
        byte[] pcBytes = BitConverter.GetBytes((Int32)pc);

        for(int i = 0; i < 4; i++)
        {
            memory[memory[1]+i] = pcBytes[i];
        }
    }

    private void SetPCSys()
    {
        byte[] pcBytes = new byte[4];

        for(int i = 0; i < 4; i++)
        {
           pcBytes[i] = memory[memory[1]+i];
        }
        pc = BitConverter.ToInt32(pcBytes);
    }

    private void LoadFSys()
    {
        //Layout: [filename] [\0] [32bit load address]
        int cur = memory[1];

        string path = "";
        while(memory[cur] != 0)
        {
            path += (char)memory[cur];
            cur++;
        }
        cur++; //At first byte of address

        
        
        byte[] writeAdrs = new byte[4];
        for(int i = 0; i < 4 ; i++)
        {
            writeAdrs[i] = memory[cur];
            cur++;
        }


        UInt32 address = BitConverter.ToUInt32(writeAdrs);

        try
        {  
            byte[] file = File.ReadAllBytes(path);
            for(int i = 0; i<file.Length; i++)
            {
                memory[address+i] = file[i];
            }
        }
        catch
        {
            ErrorHandler.Throw($"Could not load file: {path}. Does that path exist? Did you reserve enough memory?");
        }
        


    }

    private void Set32Sys()
    {
         //1 (points to value) layout: [value] [a1] [a2] [a3] [a4]
        byte val = memory[memory[1]];
        byte[] adrs = new byte[4];
        for(int i = 0; i < 4 ; i++)
        {
            adrs[i] = memory[memory[1]+1+i];
        }
        memory[BitConverter.ToUInt32(adrs)] = val;
    }

    private void Load32Sys()
    {
        //1 is expected to hold the address of the first byte of the 32 bit address
        //Layout: [mem(1)] [a2] [a3] [a4]
        byte[] adr = new byte[4];
        for(int i = 0; i < 4 ; i++)
        {
            adr[i] = memory[memory[1]+i];
        }
        //Setting 1 to loaded value:
        memory[1] = memory[BitConverter.ToUInt32(adr)]; //Using uint32 here, allows for larger adr space
    }

    private void Debug(in Instruction cur)
    {
        Console.WriteLine($"I: {cur} \n pc: {pc} \n 3: {memory[3]}");
    }

    public void Run()
    {
        GC.Collect();
        try
        {
            while(pc<code.Count)
            {
                //Debug(code[pc]);
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