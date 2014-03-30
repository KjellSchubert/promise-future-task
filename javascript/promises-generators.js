var fs = require('fs');
var Q = require('q');
var co = require('co'); // there also is Q.async() and Q.spawn(), not sure yet which lib I will like better. I had trouble with Q.async() & nesting generators, co() worked as expected out of the box.

"use strict";

// Generator yielding a list of promises.
// @return promise that resolves to file length
// If this here gives you a runtime error about the '*' make sure node --version shows 0.11+
// and make sure you start node.exe with node --harmony.
function* getFileLengthInKb(fileName) {
  var fileContent = yield Q.denodeify(fs.readFile)(fileName, {encoding:'utf8'});
  return fileContent.length / 1024;
}

co(function*() {
  var testFileName = "package.json";
  var fileLengthInKb = yield getFileLengthInKb(testFileName);
  console.log('length  of ' + testFileName + ' is ' + fileLengthInKb + ' kB');
})();


/* this here's with explicit error handling:
co(function*() {
  var testFileName = "package.json";
  try {
    var fileLengthInKb = yield getFileLengthInKb(testFileName);
    console.log('length  of ' + testFileName + ' is ' + fileLengthInKb + ' kB');
  }
  catch (ex) {
    // note that you could try-catch errors in a way that looks just like in correspondong sync code,
    // see http://wingolog.org/archives/2013/05/08/generators-in-v8 for example.
    console.log('generator caught error: ' + ex.message);
    throw ex; // rethrow is boring of course, this here just demonstrates exception handling
  }
})();
*/

console.log("this gets printed before fs.readFile() completes");