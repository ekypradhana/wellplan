//--- ECIS General Configuration
var ecisBaseUrl = window.location.protocol + "//"
        + window.location.host + "/"
        + window.location.pathname.split('/')[1]
        + "/";

var ecisHost = window.location.protocol + "//"
        + window.location.host + "/";

var ecisBaseUrl_noLastTrail = window.location.protocol + "//"
        + window.location.host + "/"
        + window.location.pathname.split('/')[1];

var ecisHost_noLastTrail = window.location.protocol + "//"
        + window.location.host;

var jsonDateFormat = "dd-MMM-yyyy";
//var cultureInfo = "en-SG";

var ecisColors = ["#317DB5", "#BCE4BE", "#FFFFC3", "#FCAE68", "#D51D26",
"#128572", "#B3ACD1", "#BBBBBB", "#D85F21", "#ADDAD5"];

function getEcisColor(iColorIndex) {
    return getArrayElement(ecisColors, iColorIndex);
}
