
TARGET=Examples/hello.vptr

run: build
	./build.out

build: $(TARGET)
	python3 compiler.py $(TARGET)
	fasm build.asm build.out
	chmod +x build.out

clean:
	-rm build.asm
	-rm build.out


