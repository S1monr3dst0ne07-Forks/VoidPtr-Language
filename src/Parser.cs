using Errors;
using Interpreter;
using Lexing;

namespace Parsing;

public class Parser
{
    private List<Token> tokens;
    private int curIndx = -1;
    private Token current;
    private HashSet<TokenType> opTypes = [TokenType.And,
                                          TokenType.Or,
                                          TokenType.Xor,
                                          TokenType.ShiftLeft, 
                                          TokenType.ShiftRight, 
                                          TokenType.Assign];

    private Dictionary<string, int> labelDict = new();
    private Dictionary<string,List<int>> labelSubscribers = new(); 

    private List<Instruction> instructions = new();

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    private Token Next()
    {
        curIndx++;
        if(curIndx<tokens.Count)
        {
            current = tokens[curIndx];
            return current;
        }
        return new(TokenType.End,"END");
    }


    private void Expect(TokenType type)
    {
        if(current.type != type)
        {
            ErrorHandler.Throw($"Expected {type}, got {current.type}",current);
        }
    }

    private void ExpectError(string msg)
    {
        ErrorHandler.Throw($"{msg}, got {current.type}",current);
    }

    public List<Instruction> MakeInstructs()
    {

        InstrValue address1 = new();
        Next();

        bool lastAddress = false; //Check if last token was a value
       
        while(current.type != TokenType.End)
        {
            switch(current.type)
            {
                case TokenType.OpenBr or TokenType.Address:
                    address1 = MakeAddress();
                    if(lastAddress)
                    {
                        ExpectError("Expected operation");
                    }
                    lastAddress = true;
                continue;
                
                case TokenType.Label:
                    MakeLabel(current.val, instructions.Count-1);
                    Next();
                    lastAddress = false;
                continue;

                case TokenType.Not:
                    instructions.Add(MakeNot());
                    lastAddress = false;
                continue;

                case TokenType.Cmp:
                    instructions.Add(MakeCmp());
                    lastAddress = false;
                continue;

                case TokenType.Jmp:
                    Next();
                    Expect(TokenType.Label);
                    Instruction inst = new()
                    {
                        type = Operator.Jmp,
                        val1 = new(AddressMode.Const, GetLabel(current.val, instructions.Count))
                    };
                    instructions.Add(inst);
                    Next();
                    lastAddress = false;
                continue;

            }

            //Double operators are generated here
            if(opTypes.Contains(current.type))
            {
                instructions.Add(MakeOp(address1));
                lastAddress = false;
            }
            else
            {
                ExpectError("Invalid expression start");
            }
        }

        //Check if all labels where defined.
        foreach(KeyValuePair<string,List<int>> subs in labelSubscribers)
        {
            if(subs.Value.Count != 0)
            {
                ExpectError($"Label '{subs.Key}' was used but never defined");
            }
        }

        return instructions;
    }

    //Define a label
    private void MakeLabel(string name, int instrIndx)
    {
        labelDict.Add(name, instrIndx);

        //Labels can be used before they are defined, so we check if anyone was already using it.
        if(labelSubscribers.TryGetValue(name, out List<int> subs))
        {
            foreach(int indx in subs) //Patch label address for all subscribers
            {
                Instruction ins = instructions[indx];
                ins.val1.value = instrIndx;
                instructions[indx] = ins;
            }
            subs.Clear();
        }
    }

    //Returns the address of the given label.
    private int GetLabel(string label, int currentIndx)
    {

        if(labelDict.TryGetValue(label, out int jmpAdrs))
        {
            //Label was already defined: return address.
            return jmpAdrs;
        }
        else
        {
            //Label was not yet defined, remember this address for future patching.
            if(labelSubscribers.TryGetValue(label, out List<int> subs))
            {
                subs.Add(currentIndx);
            }
            else
            {
                labelSubscribers[label] = [currentIndx];
            }
        }
        return 0;
    }

    private Instruction MakeAssign(InstrValue val1)
    {
        Expect(TokenType.Assign);

        Next();

        InstrValue val2 = MakeValue();

        return new Instruction(val1,val2,Operator.Assign);

    }

    private Instruction MakeNot()
    {
        Expect(TokenType.Not);
        Next();
        
        InstrValue val1 = MakeValue();
        
        
        Expect(TokenType.Assign);
        Next();
        
        InstrValue dest = MakeValue();
        

        return new Instruction(val1,dest,Operator.Not);
    }

    private Instruction MakeCmp()
    {
        Expect(TokenType.Cmp);
        Next();
        
        InstrValue val1 = MakeValue();
        

        return new Instruction(val1,new(),Operator.Cmp);

    }

    private Instruction MakeOp(InstrValue val1)
    {
        
        Operator op = Operator.And;

        if(!opTypes.Contains(current.type))
        {
            ExpectError("Expected operator.");
        }

        switch(current.type)
        {
            case TokenType.And:
                op = Operator.And;
            break;

            case TokenType.Assign:
                return MakeAssign(val1);
           
            case TokenType.Or:
                op = Operator.Or;
            break;

            case TokenType.ShiftLeft:
                op = Operator.ShiftLeft;
            break;

            case TokenType.ShiftRight:
                op = Operator.ShiftRight;
            break;

            case TokenType.Xor:
                op = Operator.Xor;
            break;
        }

        Next();
        InstrValue val2 = MakeValue();
        
        Expect(TokenType.Assign);

        Next();
        InstrValue dest = MakeValue();
        

        return new Instruction(val1,val2,op){val3 = dest};

    }

    private InstrValue MakeValue()
    {
        InstrValue val2;

        if(current.type == TokenType.AsValue)
        {
            Next();
            Expect(TokenType.Address);

            int val = int.Parse(current.val);
            
            if(val > 255)
            {
                ExpectError("Literals are only allowed to be in a 1 byte range");
            }
            
            val2 = new InstrValue(AddressMode.Const, val);
            Next();
        }
        else
        {   
            val2 = MakeAddress();
        }

        return val2;
    }

    private InstrValue MakeAddress()
    {
        if(current.type != TokenType.OpenBr && current.type != TokenType.Address)
        {
            ExpectError("Expected Address");
        }

        string val = "";
        AddressMode mode = AddressMode.Direct;
        
        if(current.type == TokenType.OpenBr)
        {
            mode = AddressMode.Indirect;
            Next();

            Expect(TokenType.Address);
            val = current.val;
            Next();

            Expect(TokenType.ClosedBr);
            Next();

            return new InstrValue(mode,int.Parse(val));
        }

        Expect(TokenType.Address);

        InstrValue value = new InstrValue(mode, int.Parse(current.val));

        Next();
        
        return value;

    }
}