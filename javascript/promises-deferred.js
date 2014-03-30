var fs = require('fs');
var Q = require('q');

"use strict";

// @return promise that resolves to file length
function getFileLengthInKb(fileName) {
  var deferred = Q.defer();
  fs.readFile(fileName, { encoding: 'utf8' }, function (err, fileContent) {
    if (err)
      deferred.reject(err);
    else
      deferred.resolve(fileContent.length / 1024);
  });
  return deferred.promise;
}

var testFileName = "package.json";
getFileLengthInKb(testFileName)
    .then(function(fileLength) {
        console.log('length  of ' + testFileName + ' is ' + fileLength + ' kB');
    })
    .done(); // without this errors would be silently swallowed by the system
