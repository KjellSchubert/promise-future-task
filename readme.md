<link href="style.css" type="text/css" rel="stylesheet"></link>

Promise, Future, Task: a comparison of async programming idioms across Javascript, C#, Python and C++
===================================================================================================

Async programming has many alternative labels, e.g. async IO, non-blocking IO, event-driven programming or [Reactive Programming](http://www.reactivemanifesto.org/). The choice between sync & async programming styles is mostly a matter of preference: if you are using sync APIs then your code will usually revolve around threads & mutexes, and as a C# developer for example you'll work with concepts like lock(), SynchronizationContexts, ThreadPools, BackgroundWorkers, Invoke/BeginInvoke, and if you're unlucky also with COM Apartment models. If you are using async APIs then your code will  revolve around abstractions like callbacks, promises, futures or async/await. Over the last years we saw a slow shift towards async APIs: see nodejs' rise in popularity, Python's recent introduction of [asyncio](http://legacy.python.org/dev/peps/pep-3156/) or .NET's [async/await](http://blogs.msdn.com/b/pfxteam/archive/2012/04/12/async-await-faq.aspx). 

The first time I worked in an environment that used callbacks & async as the preferred and practically only mode of operation was when implementing [nodejs](http://nodejs.org/)-based TCP and HTTP server applications. After having worked primarily with sync APIs over the last decade this async nodejs experience was refreshing: there was no worrying about insufficient locking of mutexes (with a threat of state corruption), deadlocks and suboptimal configuration of threadpools. Reasoning about the correctness of async code seemed a lot simpler. Working in the nodejs environment was unusually productive, and more importantly it was unusually fun :) I had been following the recent development of async programming concepts & idioms across multiple languages, resulting in this comparison of async concepts & idioms between Javascript/nodejs, C#, Python and C++ here.

Callbacks
---------

Callbacks come in two basic flavors: async & async. An example for a sync call back is Javascript's Array.prototype.reduce: 

    var foo = [1,2].reduce(function(accu, x) { return accu+x}, 0);
    console.log(foo);
    
Here the sync callback function will be invoked zero or one or many times before reduce() returns, but never after it returns. On the other hand an example for an async callback is nodejs's fs.readFile:

    fs.readFile("readme.md", {encoding:'utf8'}, 
      function(err, data) { console.log(data.length) });

Here the async callback will be invoked zero or one or many times after readFile() returns, but never before it returns. One of the most important pieces of advice about designing callback-based APIs I found here: ["CHOOSE SYNC OR ASYNC, BUT NOT BOTH"](http://blog.ometer.com/2011/07/24/callbacks-synchronous-and-asynchronous/). APIs that don't follow this design principle make reasoning about the correctness of code using them a [nightmare](http://blog.izs.me/post/59142742143/designing-apis-for-asynchrony).

Some languages are using callbacks more than others: the nodejs API uses async callbacks heavily & consistently for IO, whereas core javascript uses sync callbacks heavily all over, e.g. in [Array.prototype.forEach()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/forEach) and reduce(). Python users mostly encounter sync callbacks in <a href='http://docs.python.org/2.7/library/functions.html#map'>map()</a>, filter() and reduce(), whereas async callbacks are encountered much less frequently, with IO operations like [file.readline](http://docs.python.org/2.7/library/stdtypes.html?highlight=readlines#file.readline) being mostly sync & thus callback-free. 

Flavors of async programming
----------------------------

We have three basic flavors of async programming:

* callback-based programming
* promises-based programming (with 'promises' also being called 'futures' in some languages like Python for example)
* using promises in conjunction with generators or coroutines: [yield from](https://groups.google.com/forum/#!topic/python-tulip/bmphRrryuFk) in Python, [yield](http://stackoverflow.com/questions/2282140/whats-the-yield-keyword-in-javascript) in Javascript, [async/await](http://blogs.msdn.com/b/pfxteam/archive/2012/04/12/async-await-faq.aspx) in .NET.

Some derogatively call callback-based programming '[callback hell](http://callbackhell.com/)' suffering from the '[pyramid of doom](http://tritarget.org/blog/2012/11/28/the-pyramid-of-doom-a-javascript-style-trap/)' in Nodejs, others (few?) actually prefer callbacks over the alternative models. Personally I strongly prefer the promise-based model over callbacks, since it requires less boilerplate code around error handling. I haven't used async/await with generators in nodejs in production code yet (since nodejs --harmony 0.11.x is still in beta as of today), so it's too early for me to tell if I prefer working with promises directly or with async/await. From the little experimental work I have done with nodejs async/await I see several advantages over promises & callbacks: 

* async/await reduces the need for boilerplate code compared to promises further, code becomes more readable
* async/await makes interpreting error stack traces easier. Note that some promise libraries have utilities which can reduce the pain of tracking chains of callbacks and promises, e.g. [Q.longStackSupport](https://github.com/kriskowal/q), but these are typically crutches that shouldn't be enabled in production code.
* the biggest pitfall of Javascript promises is imo that a missing promise.done() or promise.catch() can easily result in errors being silently swallowed by the system, something which async/await can prevent.

Callback example
----------------

Quote from sample code in javascript/callbacks.js:

    // @param callback(err, len) will receive the file length
    function getFileLengthInKb(fileName, callback) {
        fs.readFile(fileName, {encoding:'utf8'}, 
          function(err, fileContent) {
            if (err) {
                callback(err);
            }
            var len = fileContent.length / 1024;
            callback(null, len);
        });
    }

    var testFileName = "promises.js";
    getFileLengthInKb(testFileName, function(err, fileLength) {
        if (err) {
            throw err;
        }
        console.log('len ' + testFileName + ' is ' + fileLength + ' kB');
    });

One of the most tedious aspects of callbacks() is the [error handling boilerplate](http://docs.nodejitsu.com/articles/errors/what-are-the-error-conventions) code: the 'err' parameter (which for nodejs is relatively consistently the 1st param of the callback func) needs to be dealt with in all (or most) callback implementations. Also: if the core of the callback implementation (in this example 'fileContent.length / 1024') could throw exceptions then the boilerplate becomes even more tedious, having to wrap the block in a try-catch to propagate the exception to the callback:

    // @param callback(err, len) will receive the file length
    function getFileLengthInKb(fileName, callback) {
        fs.readFile(fileName, {encoding:'utf8'}, function(err, fileContent) {
            if (err) {
                callback(err);
            }
            var len = undefined;
            try {
                len = fileContent.length / 1024;
            }
            catch (ex) {
                callback(ex);
            }
            if (len !== undefined)
                callback(undefined, len);
        });
    }

Without this try-catch an exception in the callback would trigger a [process.uncaughtException](http://nodejs.org/api/process.html#process_event_uncaughtexception) event, and it's strongly recommended to let this terminate the process.

Promise example
---------------

Javascript promise or 'thenable' specs went thru several iterations over the years, e.g. [Promises/A](http://wiki.commonjs.org/wiki/Promises/A), [Promises/A+](https://github.com/promises-aplus/promises-spec) and [Promises/B](http://wiki.commonjs.org/wiki/Promises/B). Some of the early adopters of promises like jQuery are unfortunately [not compatible](https://github.com/kriskowal/q/wiki/Coming-from-jQuery) with the modern promise specs. 

Quote from sample code in javascript/promises-deferred.js:

    // @return promise that resolves to file length
    function getFileLengthInKb(fileName) {
        var deferred = Q.defer();
        fs.readFile(fileName, { encoding: 'utf8' }, 
          function (err, fileContent) {
            if (err)
                deferred.reject(err);
            else
                deferred.resolve(fileContent.length / 1024);
        });
        return deferred.promise;
    }

This example shows how to integrate a callback-based API into a promise-based implementation: the [Q.defer()](https://github.com/kriskowal/q) creates the '[Deferred](http://stackoverflow.com/questions/6801283/what-are-the-differences-between-deferred-promise-and-future-in-javascript)' view of a promise, which is the producer-side view of the async action: whatever function produces a value in an async fashion promises to either resolve() or reject() the promise via deferred.reject/resolve(), returning a promise for the future value immediately via deferred.promise. The promise is passed to the consumer who can neither reject nor resolve the promise, but who can chain it with promise.then(). Luckily Deferreds are only encountered on the seams between callback-based and promise-based APIs, and promise libraries like [Q](https://github.com/kriskowal/q) can shorten the Deferred code above with utility functions like Q.denodeify(), quote from javascript/promises-denodeify.js:

    // @return promise that resolves to file length
    function getFileLengthInKb(fileName) {
        return Q.denodeify(fs.readFile)(fileName, {encoding:'utf8'})
            .then(function(fileContent) {
                return fileContent.length / 1024;
            });
    }

Here the Q.denodeify adapts the callback-based fs.readFile to a promise-based API. Note how there's very little boilerplate around 'err' anymore compared to the callback-based impl. Here the client code using the promise, chaining promises via then():

    var testFileName = "promises.js";
    getFileLengthInKb(testFileName) // returns promise
        .then(function(fileLength) { // chains promise
            console.log('len ' + testFileName + ':' + fileLength + ' kB');
        })
        .done();
        
Note the sneaky promise.done() which is easily forgotten, resulting in errors being lost in the bowels of the system, for example resulting in a pending HTTP GET that will never get a response. This somewhat (poorly) can be safeguarded against via [Q.timeout()](https://github.com/kriskowal/q). Another less frequently encountered pitfall when using promises is to forget that almost always you want to have a function (returning a promise) as an argument for promise.then(): it's relatively easy to instead pass an expression that returns a promise, which then will usually result in an unexpected execution order. So as a rule of thumb chain your promises via promise.then(function(val) { ...; return ... }) where the retval can either be a value (which then is automatically wrapped in a promise) or a promise.

Even though the promise/thenable concept is simple: for someone who never used them before it will take a few days of working with them to fully understand them. A good tutorial is [here](http://nodeschool.io/#promiseitwonthurt), detailed slides are [here](http://www.slideshare.net/domenicdenicola/callbacks-promises-and-coroutines-oh-my-the-evolution-of-asynchronicity-in-javascript).

Async/await example
-------------------

Quote from example code in javascript/promises-generators.js:

    function* getFileLengthInKb(fileName) {
        var fileContent = yield Q.denodeify(fs.readFile)(
          fileName, {encoding:'utf8'});
        return fileContent.length / 1024;
    }
    
    co(function*() {
        var testFileName = "package.json";
        var fileLengthInKb = yield getFileLengthInKb(testFileName);
        console.log('len ' + testFileName + ':' + fileLengthInKb + ' kB');
    })();

Javascript Harmony [generators](http://wingolog.org/archives/2013/05/08/generators-in-v8) support in nodejs is still in beta as of today (node 0.11+, node --harmony), [co](https://www.npmjs.org/package/co) is one of many alternative libraries that achieves C# async/await-like behavior with Javascript & generators. One of the biggest advantages of the generator/yield based coroutines is that forgetting a promise.done() no longer results in an error being accidentally suppressed & ignored. Another advantage is that someone used to sync APIs will be quickly able to work with async IO without having to learn about the intricacies and pitfalls of callbacks and promises. So what are the pitfalls? Not sure yet, probably depends on which coroutine/promise libraries you [npm install](https://www.npmjs.org/).

Comparing nodejs & C# callbacks
----------------------------------

Quote from c-sharp\HttpServerAsyncCallback.cs, using [HttpListener.GetContext](http://msdn.microsoft.com/en-us/library/vstudio/system.net.httplistener.getcontext) instead of [fs.readFile](http://nodejs.org/api/fs.html#fs_fs_readfile_filename_options_callback) as in the nodejs examples):

    IAsyncResult result = listener.BeginGetContext(new AsyncCallback(getContextCallback), listener);
    ...
      
    void getContextCallback(IAsyncResult result) 
    {
      HttpListener listener = (HttpListener)result.AsyncState;
      HttpListenerContext context = listener.EndGetContext(result);
      ...
    }

The C# syntax for the BeginXyz() / EndXyz() functions is kinda awkward imo, I cannot imagine that anyone would ever voluntarily want to program on that C# callback level. There's no need to work on that level though (and maybe the .NET designers never even intended for anyone to work on that level?), since promise-based (aka [Task](http://msdn.microsoft.com/en-us/library/vstudio/system.threading.tasks.task)-based) & [async/await](http://blogs.msdn.com/b/pfxteam/archive/2012/04/12/async-await-faq.aspx) programming are much more convenient in C#. My biggest complaint about all these callback & promise/Task-based async programming models in C# is that it can be difficult to reason about correctness in a multithreaded application, especially in [WPF](http://en.wikipedia.org/wiki/Windows_Presentation_Foundation) applications where it's typically necessary to execute some of the Tasks on workerthreads, and others on the main UI thread. Using an async programming style can be painful if on top of the hassle with async error handling & control flow you still have to worry about threading issues and mutexes.

Comparing nodejs & C# promises
------------------------------

Quote from c-sharp/HttpServerAsyncPromises.cs:

    listener.GetContextAsync().ContinueWith(processGetContextResult);
    ...
    
    static void processGetContextResult(Task<HttpListenerContext> task) 
    {
      HttpListenerContext context = (HttpListenerContext)task.Result;
      ...
    }

C# calls promises 'Tasks': <a href='http://msdn.microsoft.com/en-us/library/dd449174(v=vs.110).aspx'>System.Threading.Tasks.TaskCompletionSource</a> roughly corresponds to a nodejs Deferred ([Q.defer()](https://github.com/kriskowal/q)), and [System.Threading.Tasks.Task](http://msdn.microsoft.com/en-us/library/vstudio/system.threading.tasks.task) roughly corresponds to a thenable Javascript promise:
<table>
<tr><th></th><th>Javascript</th><th>.NET</th></tr>
<tr><td>creating Deferred</td> <td>[Q.defer()](https://github.com/kriskowal/q)</td> <td><a href='http://msdn.microsoft.com/en-us/library/dd449174(v=vs.110).aspx'>new TaskCompletionSource&lt;T&gt;()</a></td> </tr>
<tr><td>resolving a Promise</td> <td>deferred.resolve()</td> <td>TaskCompletionSource.SetResult</td></tr>
<tr><td>rejecting a Promise</td> <td>deferred.reject(ex)</td> <td>TaskCompletionSource.SetException</td></tr>
<tr><td>getting a Promise from Deferred</td> <td>deferred.promise</td> <td>TaskCompletionSource.Task</td></tr>
<tr><td>chaining promises</td> <td>promise.then()</td> <td>Task.ContinueWith()</td></tr>
</table>

Note the additional complexity of <a href='http://msdn.microsoft.com/en-us/library/dd321307(v=vs.110).aspx'>Task.ContinueWith()'s</a> TaskScheduler options, with the unwelcome (but necessary) complexity of executing these units of code on different threads. Which is especially important when it comes to UI threads: you usually want to execute the Tasks that do sync IO on worker threads, and Tasks that update the UI on the (usually main) UI thread. Besides these additional (and often painful) threading concerns for .NET Tasks Javascript promises and C# Tasks have a lot of similarities. Compare this TaskScheduler to Python's [asyncio](http://docs.python.org/dev/library/asyncio.html) which makes the [event loops](http://docs.python.org/dev/library/asyncio-eventloop.html#asyncio-event-loop) explicit.

Comparing the nodejs & .NET flavors of async/await
--------------------------------------------------

C#/.NET has syntactic sugar for async/await directly in the language: async & <a href='http://msdn.microsoft.com/en-us/library/hh156528.aspx'>await</a> are reserved language keywords. So where Javascript defines a generator via function*() .NET defines a function as 'async', and where Javascript lets the generator 'yield' a promise C# will 'await' a Task. Quote from c-sharp/HttpServerAsyncAwait.cs:

    public async static void runAsync() {
      while (...) {
        HttpListenerContext context = await listener.GetContextAsync();
        HttpListenerRequest request = context.Request;
        ...
        bool isShutdownRequest = await processRequestAsync(request, response);
        ...
        Console.WriteLine("done processing request.");
      }
    }
    async Task<bool> processRequestAsync(HttpListenerRequest request, HttpListenerResponse response) ...

So anyone who programmed with C# async/await should be feeling right at home with Javascript generators & yield and vice versa. I had only limited experience with async/await in C# so far, so I'm not sure yet about potential pitfalls. I suspect there's some complexity around thread-affinity with async/await, which hopefully disappears if you limit the use of async/await to the main (UI) thread of your application while consistently using non-blocking IO in your async functions. The only thing that makes 'async' feel like a 2nd class citizen in C# is the Async function name suffix (e.g. in [GetContextAsync](http://msdn.microsoft.com/en-us/library/vstudio/system.net.httplistener.getcontextasync)). Interestingly in nodejs citizenship is the reverse: sync calls like fs.readFileSync are supposed to feel like 2nd class citizens, and rightfully so since since they kill your server's throughput if these calls are made during the [httpserver.listen](http://nodejs.org/api/http.html#http_server_listen_port_hostname_backlog_callback) loop. Typically these xyzSync() calls in nodejs are used to load config files before the server even starts listening on TCP ports, but it's best to outlaw these potentially dangerous sync API calls in general since they are easily replaced by a Q.denodeify(fs.readFile).

Comparison of nodejs & Python promises
--------------------------------------

Python [Twisted](https://pypi.python.org/pypi/Twisted/13.2.0) is an async (or event-based) programming module that has strong parallels with nodejs, predating nodejs by many years. Some of its concepts were integrated into the Python core since Python 3.4 (released in March 2014) as the [asyncio](http://legacy.python.org/dev/peps/pep-3156/) module. A good-sized portion of the spec is necessarily dedicated to event loops and thread interaction, complexity that nodejs avoids by providing only a single application-visible event loop and thread. Imo Python is doing a much better job dealing with that complexity than C#/.NET. Surprisingly Python comes with two very similar promise implementations: [asyncio.Future](http://docs.python.org/dev/library/asyncio-task.html?highlight=asyncio.future#asyncio.Future) and the older but very similar [concurrent.futures.Future](http://docs.python.org/dev/library/concurrent.futures.html?highlight=concurrent.futures.future#concurrent.futures.Future). Which I assume duck-typing makes as interoperable as for Javascript, where [Q](https://nodejsmodules.org/pkg/q) and [when](https://nodejsmodules.org/pkg/when) promises can be mixed as long as they are thenable as specified by [Promises/A](http://wiki.commonjs.org/wiki/Promises/A). 

Interestingly Python's [asyncio.Future](http://docs.python.org/dev/library/asyncio-task.html#future) does not bother distinguishing between a Javascript-style Promise and Deferred. Practically this distinction is relatively moot for Python since there are very few core Python APIs that deal with callbacks, so unlike with nodejs there's hardly ever the need to create a Deferred and to resolve/reject it: instead sync API calls can be turned into a promise/future via [asyncio.get_event_loop().run_in_executor()](http://docs.python.org/dev/library/asyncio-eventloop.html?highlight=run_in_executor#asyncio.BaseEventLoop.run_in_executor) which executes the sync IO in a thread pool. Also: since Python has support for multithreading there may be the occasional need for mutexing shared state, e.g. via [lock](http://docs.python.org/dev/library/asyncio-sync.html#lock) also see details [here](http://docs.python.org/dev/library/asyncio-dev.html#concurrency-and-multithreading), which nodejs users don't have to worry about. Practically that shouldnt be much of a worry either as long as your Python program uses only a single event loop. 

Comparison of nodejs & Python async/await
-----------------------------------------

Both nodejs (0.11+) and Python support generators & yield, Python also has coroutines with 'yield from'. Both language use these to achieve an async/await-style chaining of promises via generators and coroutines respectively. Code snippet from python/promises.py:

    # a sync/blocking IO func
    def getNumberOfLinesInFile(fileName):
        with open(fileName) as f:
          content = f.readlines()
          return len(content)
    
    # this shows how to turn any blocking IO into a coroutine
    # via asyncio.get_event_loop().run_in_executor().
    @asyncio.coroutine
    def getNumberOfLinesInFileAsync(fileName):
        numberOfLinesInFile = yield from asyncio.get_event_loop().run_in_executor(None, getNumberOfLinesInFile, "promises.py")
        return numberOfLinesInFile
    
    # this shows nesting of coroutines
    @asyncio.coroutine
    def doAsyncStuff(fileName):
        numberOfLinesInFile = yield from getNumberOfLinesInFileAsync(fileName)
    
    # this runs a single event loop
    loop = asyncio.get_event_loop()
    loop.run_until_complete(doAsyncStuff("promises.py"))
    loop.close()

There are strong parallels between the nodejs and Python async/await flavors: the biggest difference is that Python makes the event loop explicit in loop.run_until_complete(), whereas in nodejs all user code execution happens in the single implicit event loop. Like in Javascript and C# coroutines can be nested and [chained](http://docs.python.org/dev/library/asyncio-task.html#example-chain-coroutines) in Python as well.

<table>
<tr><th></th><th>Javascript</th><th>Python</th></tr>
<tr><td>creating Deferred</td> <td>[Q.defer()](https://github.com/kriskowal/q)</td> <td>[asyncio.Future](http://docs.python.org/dev/library/asyncio-task.html#future)</td> </tr>
<tr><td>resolving a Promise</td> <td>deferred.resolve()</td> <td>future.set_result</td></tr>
<tr><td>rejecting a Promise</td> <td>deferred.reject(ex)</td> <td>future.set_exception</td></tr>
<tr><td>getting a Promise from Deferred</td> <td>deferred.promise</td> <td>not needed (Deferred and Future are the same object)</td></tr>
<tr><td>chaining promises</td> <td>promise.then()</td> <td>[future.add_done_callback](http://docs.python.org/dev/library/asyncio-task.html?highlight=add_done_callback#asyncio.Future.add_done_callback)</td></tr>
<tr><td>async/await via</td> <td>function*() generator with yield</td> <td>coroutine with 'yield from'</td></tr>
</table>

Comparison of nodejs & C++ promises
-----------------------------------

The C++ standard includes Promises since C++11 (previously in boost). Note that what C++ calls a [Future](http://en.cppreference.com/w/cpp/thread/future) corresponds to a promise/thenable in nodejs and what it calls a [Promise](http://en.cppreference.com/w/cpp/thread/promise) corresponds to a Deferred object in nodejs.

<table>
<tr><th></th><th>Javascript</th><th>C++</th></tr>
<tr><td>creating Deferred</td> <td>[Q.defer()](https://github.com/kriskowal/q)</td> <td>[std::promise](http://en.cppreference.com/w/cpp/thread/promise)</td> </tr>
<tr><td>resolving a Promise</td> <td>deferred.resolve()</td> <td>[promise.set_value](http://en.cppreference.com/w/cpp/thread/promise/set_value)</td></tr>
<tr><td>rejecting a Promise</td> <td>deferred.reject(ex)</td> <td>[promise.set_exception](http://en.cppreference.com/w/cpp/thread/promise/set_exception)</td></tr>
<tr><td>getting a Promise from Deferred</td> <td>deferred.promise</td> <td>[promise.get_future()](http://en.cppreference.com/w/cpp/thread/promise/get_future)</td></tr>
<tr><td>chaining promises</td> <td>promise.then()</td> <td>missing!</td></tr>
</table>

A major omission in the current C++ standard is that atm there is no standard way to chain C++ futures via then() or ContinueWith(), though there are 3rd party libraries for that, e.g. [task::then](http://msdn.microsoft.com/en-us/library/hh750044.aspx). Another difference between C++ [futures](http://en.cppreference.com/w/cpp/thread/future) and Javascript [Promises/A+](http://promises-aplus.github.io/promises-spec/) is in the case of resolving or rejecting an already resolved or rejected promise: Javascript makes this a NOP, while C++ will throw an exception with [promise_already_satisfied](http://en.cppreference.com/w/cpp/thread/future_errc) in [promise.set_exception](http://en.cppreference.com/w/cpp/thread/promise/set_exception). I have little experience with C++11 Futures, so not sure what the pitfalls might be: std::async makes running blocking IO operations async easy enough, I suspect the tricky part is (as in C#) to ensure not having shared state between the 2 threads (which otherwise would need to be mutexed). Not sure how simple it is to integrate async C/C++ IO libraries (like libuv or boost::asio) with std::promise either, I wasn't tempted to try this myself yet. [C++ and Beyond 2012: Herb Sutter - C++ Concurrency](http://channel9.msdn.com/Shows/Going+Deep/C-and-Beyond-2012-Herb-Sutter-Concurrency-and-Parallelism) is an interesting discussion about then-able C++ futures, listing some pitfalls around 1:13:00, e.g. with std::future's destructor doing a surprising thread::join for futures returned by std::async. With a custom implementation of then() the C++11 code looks like that (quote from cpp/promises.cpp):

    auto future1 = std::async(std::launch::async, [=](){ return countLinesSync(fileName); });
    auto future2 = then(future1, [=](int lineCount){ 
                    cout << "async call: lineCount=" << lineCount; });
    future2.wait(); // if C++ had a concept like Python's event loops then we'd wait for the loop to finish here instead.

Since there's no standard thenable promise in the C++ standard yet there is no mention of some form of async/await in the C++ standard either yet.

Summary
-------

There is a lot of convergence between async programming in Javascript/nodejs, Python, C# and to a lesser degree C++. Even if you are primarily working with promises/futures or async/await in only one of these languages it can be beneficial to explore the corresponding concepts in other languages.

License
-------

(The MIT License)

Copyright (c) 2014 Kjell Schubert

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.