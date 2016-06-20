var effectConditions;

$(document).ready(function(){
	
	$("select").change(updateResult);
	$("input").change(updateResult);

	effectConditions = loadConditions();
	updateResult();
});

function updateResult() {
	
	var result = "";
	
	var effectValue = $( "#effect option:selected" ).val();
	var difficultyValue = $( "#difficulty" ).val();
	var resistanceValue =  $( "#resistance" ).val();
	
	var difference = difficultyValue - resistanceValue;
	
	if (difference <= -5) {
		result += "Resisted!";
		$( "#result" ).removeClass().addClass("success");
	} else {
		result += "Injury";
		$( "#result" ).removeClass().addClass("fail_by_1");
		
		if (difference > 0) {
			result += ", "
		}
		
		if (difference > 0 && difference <= 5){
			result += effectConditions[effectValue][0];
			$( "#result" ).removeClass().addClass("fail_by_2");
		} else if (difference > 5 && difference <= 10) {
			result += effectConditions[effectValue][1];	
			$( "#result" ).removeClass().addClass("fail_by_3");			
		} else if (difference > 10) {
			result += effectConditions[effectValue][2];
			$( "#result" ).removeClass().addClass("fail_by_4");
		}
	}
	
	$( "#result" ).html(result);
}

function loadConditions() {
	
	var effectConditions = new Array();
	
	effectConditions["damage"] = new Array("Dazed", "Staggered", "Incapacitated");
	effectConditions["charm"] = new Array("Entranced", "Compelled", "Controlled");
	effectConditions["sleep"] = new Array("Fatigued", "Exhausted", "Asleep");
	effectConditions["paralyze"] = new Array("Hindered", "Stunned", "Paralyzed");
	effectConditions["weaken"] = new Array("Trait -2", "Trait -5", "Trait Useless");
	
	return effectConditions;
}