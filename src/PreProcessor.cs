using Errors;
using Lexing;

namespace Parsing;

/// <summary>
/// The pre-processor is used to expand and process macros,
/// performing the last operations on the token list before it gets passed to the parser. 
/// </summary>
public class PreProcessor
{
    private List<Token> tokens;
    private List<Token> processed = new(); //Output token list.

    private int curIndx = -1;
    private Token current;
    private Dictionary<string, List<Token>> macroDict = new();

    private int globalMacroCount = 0; //Used to generate unique names for local labels.

    public PreProcessor(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    private void Expect(TokenType type)
    {
        if(current.type != type)
        {
            ErrorHandler.Throw($"Expected {type}, got {current.type}",current);
        }
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

    public List<Token> Process()
    {

        Next();
        while(current.type != TokenType.End)
        {
            if(current.type == TokenType.Macro)
            {
                MakeMacro();
            }
            else if(current.type == TokenType.Name)
            {
                //Try to expand macro
                foreach(Token t in ExpandDef(current.val))
                {
                    processed.Add(t);
                }
            }
            else
            {
                //Add unimportant token
                processed.Add(current);
            }
            Next();
        }
        processed.Add(new(TokenType.End,"END")); //Add new END
        return processed;
    }

    private void MakeMacro()
    {
        Expect(TokenType.Macro);

        if(current.val == "def")
        {
            MakeDef();
        }       
        else if(current.val == "paste")
        {
            MakePaste();
        }
        else if(current.val == "stream")
        {
            MakeStream();
        }
        else
        {
            ErrorHandler.Throw($"Unknown macro type '{current.val}'",current);
        }
    }

    private void MakeStream()
    {
        Next();
        Expect(TokenType.Address);

        int streamAdr = int.Parse(current.val); //Vptr address to start writing to.
        Next();

        //Make assign for each const value
        while(current.type == TokenType.AsValue)
        {
            Next();
            Expect(TokenType.Address);
            
            processed.Add(new(TokenType.Address,$"{streamAdr}",current.line,current.file));
            processed.Add(new(TokenType.Assign,"->",current.line,current.file));
        
            processed.Add(new(TokenType.AsValue,"$",current.line,current.file));
            processed.Add(current);
            Next();
            streamAdr++;

        }
        
        Expect(TokenType.EndMacro);
    }

    private void MakePaste()
    {
        Next();
        Expect(TokenType.Name);
        
        string code;
        try
        {
            code = File.ReadAllText(current.val);
        }
        catch
        {
            ErrorHandler.Throw($"Could not find file '{current.val}' to paste.",current);
            return;
        }

        //Getting file tokens by creating a new lexer.
        Lexer l = new(code, current.val); //current.val holds the prev. loaded file name.
        int cur = curIndx+1; //+1 because insert pushes elements towards end.

        //Insert each token from file, time complexity can be terrible here.
        foreach(Token t in l.MakeTokens())
        {
            if(t.type == TokenType.End) {break;}
            tokens.Insert(cur,t);
            cur++;
        }
        
    }

    //ExpandDef Expands a macro and returns a fully expanded token list (no name toks).
    private List<Token> ExpandDef(string macName)
    {
        globalMacroCount++; //Increase global macro id
        int currentMacroID = globalMacroCount; //Save id for current scope

        List<Token> expanded = new();

        //Get Tokens from macro dict.
        if(macroDict.TryGetValue(macName, out List<Token> macroTokens))
        {
            foreach(Token t in macroTokens)
            {
                //Macro in macro: Recurse.
                if(t.type == TokenType.Name)
                {
                    foreach(Token rT in ExpandDef(t.val))
                    {
                        expanded.Add(rT);
                    }
                    continue;
                }
                else if(t.type == TokenType.Label && t.val.StartsWith("__")) //Local labels start with __
                {
                    //Modifiying name of local label.
                    Token copy = t;
                    copy.val = $"{currentMacroID}{copy.val}";
                    expanded.Add(copy);
                    continue;
                }
                expanded.Add(t);
            }
        }
        else
        {
            ErrorHandler.Throw($"Macro '{current.val}' not defined in current state.", current);
        }


        return expanded;
    }

    private void MakeDef()
    {
        Next();
        Expect(TokenType.Name);

        if(macroDict.ContainsKey(current.val))
        {
            ErrorHandler.Throw($"Redefenition of macro '{current.val}'",current);
        }

        List<Token> macToks = new(); //Tokens for this macro.
        macroDict.Add(current.val, macToks); //Adding new entry for this macro.

        Next(); //Adding all Tokens inside the macro to macro tokens.
        while(current.type != TokenType.EndMacro && current.type != TokenType.End)
        {
            
            if(current.type == TokenType.Macro)
            {
                ErrorHandler.Throw($"You aren't powerfull enough to wield recursive macros, mortal.", current);
            }

            macToks.Add(current);
            Next();
        }
        Expect(TokenType.EndMacro);
    }
}
