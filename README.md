# Benchmarks Game

Style and performance improvements for the C# and F# entries in the benchmarks game.

## C#

http://benchmarksgame.alioth.debian.org/u64q/csharp.html

### Mandelbrot

Before:

C#      7.29s  
Java    7.10s  
 
Laptop: Improved C# Mandelbrot faster than original. f1 (2838.6786 ± 2.4023 ms) is ~62.6% faster than f2 (7585.2725 ± 171.9895 ms).

Submitted and accepted

Now:

C#      6.79s  
Java    7.10s  


### NBody

Before:

C#      21.70s  
Java    21.54s  

Improved C# NBody faster than original. f1 (6291.6044 ± 5.6276 ms) is ~4.1% faster than f2 (6557.9256 ± 7.2672 ms).

Submitted and accepted but 21.95s, what the heck.

Can't make this faster. Put back the pairs and tried to make it parallel but this was slowing.

Stumped. Best I submitted was 21.80s.


### Reverse-Complement

Before:

C#      1.39s  
Java    1.10s  

Improved by reversing in place and reusing byte arrays. Hard to test performance as involves large file load.

Submitted. Hopeful...


### Frankuch-Redux

I can't improve this. It seems small array manipulation is more costly in .Net than Java?

### K-Nucleotide

Working on it. Waiting for Reverse-Complement result as I will reuse some of the code/ideas.



