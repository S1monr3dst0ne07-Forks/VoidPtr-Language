


import sys
import os
from typing import Literal, Any
from dataclasses import dataclass as dc
import itertools
import subprocess


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

    def size(self):
        return self.number if self.kind in ('direct', 'indirect') else 0

    def load(self, emit, reg):
        addr = self.number * 4
        match self.kind:
            case 'lit':      emit(f'mov {reg}, {self.number}')
            case 'direct':   emit(f'mov {reg}, [mem + {addr}]')
            case 'indirect': 
                emit(f'mov {reg}, [mem + {addr}]')
                emit(f'mov {reg}, [mem + {reg}]')


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
            b = AstValue.parse(stream)

        stream.expect('->')
        dst = int(stream.pop())

        return cls(a, b, op, dst)

    def size(self):
        return max(
            self.a.size(), 
            self.b.size() if self.b is not None else 0,
            self.dst
        )

    def compile(self, emit):
        self.a.load(emit ,"rax")

        if self.b is not None:
            reg = "cl" if self.op in ('shl', 'shr') else "rbx"
            self.b.load(emit, reg)
            emit(f"{self.op} rax, {reg}")

        emit(f'mov [mem + {self.dst*4}], rax')



@dc
class AstLabel:
    name : str

    @classmethod
    def parse(cls, stream):
        return cls(stream.pop())

    def size(self):
        return 0

    def compile(self, emit):
        emit(f'{self.name}:')


@dc
class AstJump:
    name : str

    @classmethod
    def parse(cls, stream):
        stream.expect("'")
        return cls(stream.pop())

    def size(self):
        return 0

    def compile(self, emit):
        emit(f'jmp {self.name}')


skip_gen = itertools.count(0)

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

    def size(self):
        return max(self.cond.size(), self.target.size())

    def compile(self, emit):
        skip_label = f"skip{next(skip_gen)}"

        self.cond.load(emit, 'rax')
        emit(f"cmp rax, 0")
        emit(f"jne {skip_label}")
        self.target.compile(emit)
        emit(f"{skip_label}:")


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
        return cls(nodes)

    def size(self):
        return max(x.size() for x in self.nodes)

    def compile(self, emit):
        for node in self.nodes:
            node.compile(emit)




def main():
    path = sys.argv[1] if len(sys.argv) > 1 else 'main.vptr'
    if not os.path.isfile(path):
        print(f"Error: No such file: `{path}`")
        sys.exit(1)

    asm = []
    emitter = lambda x: asm.append(x)

    #actual compile 
    stream = lex(path)
    root = AstProg.parse(stream)

    #fasm header
    mem_size = root.size()
    emitter("format ELF64 executable")
    emitter("entry start")
    emitter(f"mem: rq {mem_size}")
    emitter("segment readable executable")
    emitter("""
sys:
    cmp qword [mem], 1
    je sys_print
    ret

sys_print:
    mov rax, [mem + 1]
    mov rax, [mem + rax]
            """)

    emitter("start:")

    root.compile(emitter)

    emitter("mov rdi, 0")
    emitter("mov rax, 60")
    emitter("syscall")

    build_path = 'build.asm'
    out_path   = 'build.out'
    with open(build_path, 'w') as f:
        f.write('\n'.join(asm))

    subprocess.run(['fasm', build_path, out_path])
    subprocess.run(['chmod', '+x', out_path])



if __name__ == "__main__":
    main()




