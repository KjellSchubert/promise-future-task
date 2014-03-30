import asyncio

# a sync/blocking IO func
def getNumberOfLinesInFile(fileName):
    with open(fileName) as f:
      content = f.readlines()
      print("getNumberOfLinesInFile: done reading %s with %d lines" % (fileName, len(content)))
      return len(content)

# This shows how to turn any blocking IO into a coroutine
# via asyncio.get_event_loop().run_in_executor().
@asyncio.coroutine
def getNumberOfLinesInFileAsync(fileName):
    numberOfLinesInFile = yield from asyncio.get_event_loop().run_in_executor(None, getNumberOfLinesInFile, "promises.py")
    print("getNumberOfLinesInFileAsync: done reading %s with %d lines" % (fileName, numberOfLinesInFile))
    return numberOfLinesInFile

# this shows nesting of coroutines
@asyncio.coroutine
def doAsyncStuff(fileName):
    numberOfLinesInFile = yield from getNumberOfLinesInFileAsync(fileName)
    print("doAsyncStuff: done reading %s with %d lines" % (fileName, numberOfLinesInFile))

# this runs a single event loop
loop = asyncio.get_event_loop()
loop.run_until_complete(doAsyncStuff("promises.py"))
loop.close()