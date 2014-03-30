var fs = require('fs');
var Q = require('q');

"use strict";

// @return promise that resolves to file length
function getFileLengthInKb(fileName) {
  return Q.denodeify(fs.readFile)(fileName, {encoding:'utf8'})
    .then(function (fileContent) {
        return fileContent.length / 1024;
    });
}

var testFileName = "package.json";
getFileLengthInKb(testFileName)
    .then(function(fileLength) {
        console.log('length  of ' + testFileName + ' is ' + fileLength + ' kB');
    })
    .done(); // without this errors would be silently swallowed by the system
