<h1>VoidPtr</h1>
<img src = "https://github.com/TheGameGuy2/ByteLang/blob/main/void_ptr_logo.png" </img>
<h3>An eso lang allowing only logical bitwise operations on single bytes</h3>
<p>This project was made for CS class. <br> The source of this documentation is src/Documentation.txt</p>
<h2>Usage</h2>

<ul>
<li>The CLI accepts 0 or 1 arguments.</li>
<li>If no argument is passed, VoidPtr searches for a file named <code>main.vptr</code>.</li>
<li>If an argument is passed, VoidPtr expects it to be a file path.</li>
<li>VoidPtr always generates token and instruction dump files for debugging purposes.</li>
</ul>

<hr>

<h2>Comments</h2>

<p>Comments are enclosed in semicolons.</p>

<pre><code>;This is a comment;</code></pre>

<hr>

<h2>Addresses</h2>

<p>
VoidPtr only allows you to work with single bytes in memory.
Bytes are addressed by writing their numeric address.
</p>

<h3>Example</h3>

<pre><code>3 -> 4</code></pre>

<p>Copies the value from byte <code>3</code> into byte <code>4</code>.</p>

<hr>

<h2>Constants</h2>

<p>
Constants begin with <code>$</code> and are one byte wide.
The valid range is 0–255.
</p>

<pre><code>3 -> $42</code></pre>

<p>Sets byte <code>3</code> to the value <code>42</code>.</p>

<hr>

<h2>Indirect Addressing</h2>

<p>
A byte can be used as a pointer to another address.
</p>

<pre><code>3 -> $6

[3] -> 4</code></pre>

<p>
The first instruction stores the value <code>6</code> in byte <code>3</code>.
The second instruction uses that value as an address.
</p>

<div class="note">
Note:As you may notice, this limits the address space of pointers to 255 bytes,
higher addresses are accessible by system calls. Direct access is not limited.
</div>

<hr>

<h2>Logical Operations</h2>

<p>All logical operations are performed bitwise.</p>

<h3>Double Operations</h3>

<p>Structure</p>

<pre><code>[a] operator [b] -> [destination]</code></pre>

<ul>
<li><code>a</code> must be an address.</li>
<li>The destination must be an address.</li>
<li><code>b</code> can be an address or a constant.</li>
</ul>

<h4>Example</h4>

<pre><code>2 -> $1
3 -> $0

2 &amp; 3 -> 4

2 &amp; $1 -> 4</code></pre>

<p>
The first operation performs a bitwise AND between addresses 2 and 3.
The second performs the same operation using a constant.
</p>

<h4>Supported Double Operators</h4>

<ul>
<li><code>&amp;</code> - Bitwise AND</li>
<li><code>|</code> - Bitwise OR</li>
<li><code>^</code> - Bitwise XOR</li>
<li><code>&lt;</code> - Left shift</li>
<li><code>&gt;</code> - Right shift</li>
<li><code>-></code> - Assignment</li>
</ul>

<p>
For shift operations, the second argument specifies the shift amount.
</p>

<hr>

<h3>Single Operations</h3>

<p>Structure</p>

<pre><code>operator a -> destination</code></pre>

<ul>
<li><code>a</code> can be an address or constant.</li>
<li>The destination must be an address.</li>
</ul>

<h4>Example</h4>

<pre><code>! 2 -> 3

! $1 -> 4</code></pre>

<h4>Supported Single Operators</h4>

<ul>
<li><code>!</code> - Bitwise NOT</li>
</ul>

<hr>

<h2>Compare Operation</h2>

<p>Structure</p>

<pre><code>? address</code></pre>

<p>
The compare instruction skips the next instruction if the value at the specified address is not zero.
</p>

<h3>Example</h3>

<pre><code>? 3
2 -> $5
2 -> $7</code></pre>

<p>Can be read as:</p>

<pre><code>if (3 == 0)
{
    2 -> $5
}
else:
    2 -> $7
    ...
</code></pre>

<hr>

<h2>Labels</h2>

<p>
Labels mark locations in code.
</p>

<pre><code>_start
    ...
_end</code></pre>

<p>
Labels beginning with a single underscore are global and can be jumped to from anywhere.
</p>

<hr>

<h2>Jump</h2>

<p>Structure</p>

<pre><code>'label</code></pre>

<p>Example:</p>

<pre><code>'_start</code></pre>

<p>Transfers execution to the specified label.</p>

<hr>

<h2>System Calls</h2>

<p>
System calls allow communication with the interpreter.
</p>

<p>
Memory locations <code>0</code> and <code>1</code> are reserved:
</p>

<ul>
<li>0: System call type</li>
<li>1: Pointer to arguments</li>
</ul>

<div class="note">
Important: Memory address <code>0</code> is automatically reset to <code>$0</code> after every system call.
</div>

<h3>Available System Calls</h3>

<table border="1" cellpadding="8" cellspacing="0">
<tr>
<th>Value</th>
<th>Description</th>
</tr>

<tr>
<td>$1</td>
<td>Print byte <code>n</code> as a number.</td>
</tr>

<tr>
<td>$2</td>
<td>Print byte <code>n</code> as a character.</td>
</tr>

<tr>
<td>$3</td>
<td>Read one character into byte <code>n</code>.</td>
</tr>

<tr>
<td>$4</td>
<td>Allocate <code>n</code> additional bytes.</td>
</tr>

<tr>
<td>$5</td>
<td>Deallocate <code>n</code> bytes.</td>
</tr>

<tr>
<td>$6</td>
<td>Read a 32-bit address from bytes <code>n</code> through <code>n+3</code> and load its value into address 1.</td>
</tr>

<tr>
<td>$7</td>
<td>Store the value at <code>n</code> into the 32-bit address stored in the next four bytes.</td>
</tr>

<tr>
<td>$8</td>
<td>Store the current program counter into four bytes beginning at <code>n</code>.</td>
</tr>

<tr>
<td>$9</td>
<td>Read four bytes as a 32-bit integer and set the program counter.</td>
</tr>

<tr>
<td>$10</td>
<td>Load a file into memory.</td>
</tr>

</table>

<h3>Example</h3>

<pre><code>2 -> $65

1 -> $2

0 -> $2</code></pre>

<p>
Prints the character A.
</p>

<hr>

<h2>Macros</h2>

<p>
Macros instruct the compiler to generate code automatically.
Every macro begins with <code>#</code>.
</p>

<h3>#def</h3>

<pre><code>#def reset_mem
    2 -> $0
    3 -> $0
    4 -> $0
    5 -> $0
#end</code></pre>

<p>Usage:</p>

<pre><code>? 3 '_reset
'_start

_reset
reset_mem</code></pre>

<h3>Macros inside Macros</h3>

<pre><code>#def print_3
    1 -> $3
    0 -> $1
#end

#def my_macro
    3 -> $5
    print_3

    3 -> $2
    print_3
#end</code></pre>

<h3>Local Labels</h3>

<p>
Single underscore labels are global:
</p>

<pre><code>_name</code></pre>

<p>
Double underscore labels are local to one macro expansion:
</p>

<pre><code>__name</code></pre>

<hr>

<h3>#paste</h3>

<p>
Equivalent to <code>#include</code> in C.
The referenced file is inserted at compile time.
</p>

<pre><code>#paste file2.vptr

macro_from_other_file</code></pre>

<hr>

<h3>#stream</h3>

<p>
Writes multiple constant values to memory.
</p>

<pre><code>#stream 8 $65 $66 $67 #end</code></pre>

<p>Compiles to:</p>

<pre><code>8  -> $65
9  -> $66
10 -> $67</code></pre>

<hr>

<h2>Strings</h2>

<p>
String literals are compiled into constant ASCII values.
</p>

<h3>Example</h3>

<pre><code>2 -> "A"</code></pre>

<p>Compiles to:</p>

<pre><code>2 -> $65</code></pre>

<h3>Strings with #stream</h3>

<pre><code>#stream 2 "Hello World" $0 #end</code></pre>

<p>Compiles to:</p>

<pre><code>#stream 2
$72
$101
$108
$108
$111
$32
$87
$111
$114
$108
$100
$0
#end</code></pre>
