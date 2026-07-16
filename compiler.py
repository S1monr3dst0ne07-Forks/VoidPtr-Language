import sys
import os
from typing import Literal, Any
from dataclasses import dataclass as dc


def lex(path):
    class Streamer:
        def __init__(self, toks):
            self.toks = toks

        def peek(self, offset=0):
            return self.toks[offset] if len(self.toks) > offset else None

        def pop(self):
            return self.toks.pop(0)

        def has(self):
            return len(self.toks) > 0

        def expect(self, should):
            be = self.pop()
            if should != be:
                print(f"Error: Expected `{should}` got `{be}`")
                sys.exit(1)

    def tokenize(path):
        with open(path, 'r') as f:
            src = f.read()

        def get(char):
            match char:
                case x if x.isdigit(): return 'iden'
                case x if x.isalpha(): return 'iden'
                case '_': return 'iden'
                case ' ' | '\t' | '\n' | '\r': return 'format'
                case ';': return 'comment'
                case '\0': return 'terminator'
                case _: return 'symb'

        state = None 
        buffer = ''
        comment = False
        toks = []
        for char in src + '\n':
            kind = get(char)

            if kind == 'comment': 
                comment = not comment
                buffer = ''
                continue

            if kind != state and not comment:
                if state != 'format' and buffer:
                    toks.append(buffer)
                buffer = '' 

            buffer += char
            state = kind

        return toks

    def preprocess(raw):
        defs = {}  # macro name -> macro content
        usage = {} # marco name -> usage count (for local labels)

        def instance(name): #create instance of macro 
            out = []
            for tok in defs[name]:
                if tok.startswith('__'):
                    tok += f"_inst{usage[name]}"
                out.append(tok)
            usage[name] += 1
            return out

        def get():
            nonlocal raw, defs
            tok = raw.pop(0)

            #auto expand
            if tok in defs:
                raw[:0] = instance(tok)
                return get()

            return tok

        def run():
            nonlocal raw, defs
            out = []
            while raw:
                tok = get()
                if tok != '#': #normal token
                    out.append(tok)
                    continue

                #proprocessor prefix
                match get():
                    case 'def':
                        name = get()
                        defs[name] = run()
                        usage[name] = 0
                    case 'end':
                        break

            return out

        return run()

    return Streamer(preprocess(tokenize(path)))




@dc
class AstValue:
    number : int
    kind   : Literal['direct', 'indirect', 'lit']

    @classmethod
    def parse(cls, stream):
        match stream.pop():
            case '[':
                x = stream.pop()
                stream.expect(']')
                return cls(int(x), 'indirect')
            case '$':
                x = stream.pop()
                return cls(int(x), 'lit')
            case x:
                return cls(int(x), 'direct')


ops = { '&':'and', '|':'or', '^':'xor', '<':'shl', '>':'shr' }

@dc
class AstAssign:
    a : AstValue
    b : AstValue
    op : Literal['and', 'or', 'xor', 'shl', 'shr', 'none']
    dst : int

    @classmethod
    def parse(cls, stream):
        a = AstValue.parse(stream)
        b = None
        op = 'none'
        
        if stream.peek() in ops:
            op = ops[stream.pop()]
            a = AstValue.parse(stream)

        stream.expect('->')
        dst = int(stream.pop())

        return cls(a, b, op, dst)




@dc
class AstLabel:
    name : str

    @classmethod
    def parse(cls, stream):
        return cls(stream.pop())

@dc
class AstJump:
    name : str

    @classmethod
    def parse(cls, stream):
        stream.expect("'")
        return cls(stream.pop())

@dc
class AstBranch:
    cond : AstValue
    target : Any

    @classmethod
    def parse(cls, stream):
        stream.expect('?')
        cond = AstValue.parse(stream)
        target = AstProg.parse_node(stream)
        return cls(cond, target)


@dc
class AstProg:
    nodes : list

    @staticmethod
    def parse_node(stream):
        match stream.peek():
            case x if x.startswith('_'): return AstLabel.parse(stream)
            case "'":                    return AstJump.parse(stream)
            case '?':                    return AstBranch.parse(stream)
            case _:                      return AstAssign.parse(stream)


    @classmethod
    def parse(cls, stream):
        nodes = []
        while stream.has() and (
                (node := cls.parse_node(stream))
                is not None
        ): nodes.append(node)
        return nodes



def main():
    path = sys.argv[1] if len(sys.argv) > 1 else 'main.vptr'
    if not os.path.isfile(path):
        print(f"Error: No such file: `{path}`")
        sys.exit(1)

    stream = lex(path)
    root = AstProg.parse(stream)
    print(root)



if __name__ == "__main__":
    main()




