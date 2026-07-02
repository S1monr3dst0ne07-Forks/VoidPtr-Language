using Errors;

namespace Lexing;


public enum TokenType : byte
{
    Address,
    Label,
    Macro,
    EndMacro,
    Value,
    Name,
    OpenBr,
    ClosedBr,
    Assign, // ->
    AsValue, // $ 
    And,
    Or,
    Xor,
    Not,
    Cmp,
    Jmp,
    ShiftLeft,
    ShiftRight,
    End

}

public struct Token
{
    public TokenType type;
    public string val = "null";
    public string file = "";
    public int line = -1;

    public Token(TokenType typ, string val, int line, string sourceFile)
    {
        type = typ;
        this.val = val;
        this.line = line;
        file = sourceFile;
    }

    public Token(TokenType typ, string val)
    {
        type = typ;
        this.val = val;
    }

    public override string ToString()
    {
        return $"[{type} : {val}]";
    }
}

public class Lexer
{
    private string code;
    private string fileName;
    private int curIndx = -1;
    private char current;

    private string numbers = "0123456789"; 
    private Dictionary<char,Token> opMap = new();    
    private Dictionary<string, Token> keywordMap = new();
    private HashSet<char> skipChars;
    private int lineCount = 1;

    public Lexer(string code, string fileName)
    {
        this.code = code;
        this.fileName = fileName;

        skipChars = ['\n',' ','\t','\r'];

        opMap.Add('[',new(TokenType.OpenBr,"["));
        opMap.Add(']',new(TokenType.ClosedBr,"]"));
        opMap.Add('&',new(TokenType.And,"&"));
        opMap.Add('|',new(TokenType.Or,"|")); 
        opMap.Add('!',new(TokenType.Not,"!"));
        opMap.Add('<',new(TokenType.ShiftLeft,"<"));
        opMap.Add('>',new(TokenType.ShiftRight,">"));
        opMap.Add('?',new(TokenType.Cmp,"?"));
        opMap.Add('^',new(TokenType.Xor,"^"));
        opMap.Add('\'',new(TokenType.Jmp,"'"));

        keywordMap.Add("->",new(TokenType.Assign,"->"));
    }

    

    private char Next()
    {
        curIndx++; 
        if(curIndx<code.Length)
        {

            if(current == '\n') {lineCount++;}
            
            current = code[curIndx];
            return code[curIndx];
        }
        //We return \0 every time Next() gets out of range.
        //Error handling is passed on to the caller.
        current = '\0'; 
        return '\0';
    }

    public List<Token> MakeTokens()
    {
        List<Token> tokens = new();


        while(Next() != '\0')
        {
            if(current == ';')
            {
                MakeComment();
            }
            else if(opMap.TryGetValue(current,out Token tok)) //Is in operator dict?
            {
                //Copy proper line and file into token.
                Token tcpy = tok;
                tcpy.line = lineCount;
                tcpy.file = fileName; 
                tokens.Add(tcpy);
            }
            else if(skipChars.Contains(current))
            {
                continue;
            }
            else
            {
                if(current == '#')
                {
                    tokens.Add(MakeMacro());

                    //Next is called at the start of the loop,
                    //MakeMacro ends at next unrelated, which would get skipped.
                    curIndx--;
                }
                else if(current == '$')
                {
                    tokens.Add(new(TokenType.AsValue,"$",lineCount,fileName));
                }
                else if(current == '"')
                {
                    MakeString(tokens);
                }
                else if(numbers.Contains(current))
                {
                    tokens.Add(new(TokenType.Address,MakeNum(),lineCount,fileName));
                    curIndx--;
                }
                else if(current == '_')
                {
                    tokens.Add(MakeLabel());
                    curIndx--;
                }
                else
                {
                    tokens.Add(MakeKeyword());
                    curIndx--;
                }
            }

        }
        //Always end token list with END
        tokens.Add(new(TokenType.End,"END"));
        return tokens;
    }

    private void MakeString(List<Token> tokenList)
    {
        Next();
        while(current != '"' && current != '\0')
        {
            //Converting all chars into const values
            tokenList.Add(new(TokenType.AsValue,"$",lineCount,fileName));
            tokenList.Add(new(TokenType.Address,$"{(int)current}",lineCount,fileName));
            Next();
        }
    }

    private void MakeComment()
    {
        //Keeping track of comment start in case of unclosed comment error
        int start = lineCount;
        
        while(Next() != ';')
        {
            if(current == '\0')
            {
                ErrorHandler.Throw($"Unclosed comment in {fileName}.", start);
            }
        }
    }

    private Token MakeMacro()
    {
        string macName = "";
        Next();

        while(current != '\0' && !skipChars.Contains(current))
        {
            macName += current;
            Next();
        }

        if(macName == "end")
        {
            return new Token(TokenType.EndMacro,"end");
        }
        return new Token(TokenType.Macro, macName, lineCount, fileName);


    }

    private string MakeNum()
    {
        string num = $"{current}";
        while(numbers.Contains(Next()))
        {
            num+=current;
        }
        return num;
    }

    private Token MakeLabel()
    {
        string lName = $"{current}";
        Next();

        while(!skipChars.Contains(current) && current != '\0')
        {
            lName += current;
            Next();
        }
        
        return new Token(TokenType.Label, lName, lineCount,fileName);
    }

    private Token MakeKeyword()
    {
        string word = $"{current}";
        Next();

        while(!skipChars.Contains(current) && current != '\0' && current != '[' && current !=']')
        {
            word+=current;
            Next();
        }

        //Check if it is a known keyword, else make name.
        if(keywordMap.TryGetValue(word, out Token tok)) 
        {
            Token tcpy = tok;
            tcpy.line = lineCount;
            tcpy.file = fileName;
            return tcpy;
        }
        else
        {
            return new(TokenType.Name, word, lineCount,fileName);
        }
    }

}



