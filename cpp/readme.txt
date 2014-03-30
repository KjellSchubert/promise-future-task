I verified this with VS2012 on Windows and clang 3.5 on Linux (clang++ -pthread -std=c++11 promises.cpp -o promises).

To run on Windows with MinGW installed (mingw32-base and mingw-gcc-g++):
>set PATH=%PATH%;C:\MinGW\bin
>g++.exe -std=c++11 promises.cpp -o promises.exe

To run on Windows with Clang 3.4 & MinGW installed:
>set PATH=%PATH%;"c:\Program Files (x86)\LLVM\bin"
>clang++.exe -std=c++11 promises.cpp -o promises.exe

You'll notice this won't work with MinGW or Clang on Windows yet, see http://stackoverflow.com/questions/10209871/c11-stdasync-doesnt-work-in-mingw. Might work on Linux, but I didnt bother trying this out on Linux yet.
