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

DONE. NOW FASTER THAN JAVA.

### NBody

Before:

C#      21.70s   
Java    21.54s  

Improved C# NBody faster than original. f1 (6291.6044 ± 5.6276 ms) is ~4.1% faster than f2 (6557.9256 ± 7.2672 ms).

Submitted and accepted but 21.95s, what the heck.

Can't make this faster. Put back the pairs and tried to make it parallel but this was slow.

Stumped. Best I submitted was 21.80s.

Using more local variables as the Java version works well for me but not the test machine.
It's too close to measure in 5 runs given the variability. I think my last one beats Java but doesn't show in the test results.

Rescaled velocity to move dt out of the loop. Works well but not accepted as changes the algo.

Need a bigger improvement to show easily but not possible in such a close low level calc.

FAIL. I GIVE UP.

Not quite. Thinking I only need a 1% improvement. Will try a few very safe optimisations.


### Reverse-Complement

Before:

C#      1.39s   
Java    1.10s  

Improved by reversing in place and reusing byte arrays. Hard to test performance as involves large file load.

Submitted and accepted

Now:

C#      1.16s  
Java    1.10s  

Submitted again with improvements.

Now:

C#      0.80s  
Java    1.10s  

DONE. NOW FASTER THAN JAVA.

### Frannkuch-Redux

Before:

C#      18.80s  
Java    13.74s  

Thread improved, rotate and small array optimisations. More than enough to beat the Java time on my machine.

Now:

C#      16.97s  
Java    13.74s  

Need a good idea to make this better.

### K-Nucleotide

Java code cheats and has a very bespoke dictionary from an obscure lib.

Working on it. Waiting for Reverse-Complement result as I will reuse some of the code/ideas. Not rolling like Java could be an idea if it makes it more parallel.

Before:

C#      13.76s  
Java     7.93s  

Submitted. Should be around the Java number.

### Regex-Redux

Can't get anywhere near Java here. Regex isn't compiled in .Net Core.

Before:
C#      32.02s  
Java    12.31s  

Submitted some ordering optimisations but can only go to 30 something.

Now:
C#      31.64s  
Java    12.31s  

DONE. CANT BEAT JAVA ON REGEX.