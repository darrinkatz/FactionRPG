
function finalize_turn(path) {
  $.ajax({
    type: 'PATCH',
    url: path,
    dataType: "json",
    success: printOrderResults,
    error: function() { debugger; }
  });
}

function printOrderResults(turn) {
  $.each(turn.orders, function(index, order) {
    var report = order.asset + " " +
                 order.action.toLowerCase() + "ed " +
                 order.target + ": " +
                 capitalize(order.result) + "!";
    $(".results").append("<p>" + report + "</p>");
  });
}

function capitalize(str) {
  return str.slice(0,1).toUpperCase() + str.slice(1);
}
