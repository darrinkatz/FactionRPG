var data;

$(document).ready(function(){
	
	$("select").change(updateOutcome);
	$("input").change(updateOutcome);

	data = loadData();
	updateOutcome();
});

function updateOutcome() {
	
	var outcome = "";
	
	var checkValue = $( "#check option:selected" ).val();
	var difficultyValue = $( "#difficulty" ).val();
	var resultValue =  $( "#result" ).val();
	
	var difference = resultValue - difficultyValue;
	
	if (difference >= 0) {
		
		var numSuccesses = Math.floor((difference + 5) / 5);
		
		$( "#outcome" ).removeClass().addClass("success");
		
		if (checkValue == "attack") {
			if (numSuccesses == 1) {
				outcome += data["attack"][0];
			} else if (numSuccesses == 2) {
				outcome += data["attack"][1];
			} else {
				outcome += data["attack"][2];
			}
		} else if (checkValue == "skill") {
			outcome += numSuccesses + " degrees of Success";
		} else {
			outcome += "Resisted";
		}
		
	} else {
		
		var numFailures = Math.floor(difference / 5) * -1;

		if (checkValue == "attack") {
			outcome += "Miss";
			$( "#outcome" ).removeClass().addClass("fail_by_4");
		} else if (checkValue == "skill") {
			outcome += numFailures + " degrees of Failure";
			$( "#outcome" ).removeClass().addClass("fail_by_4");
		} else {
			if (difference < 0 && difference >= -5){
				outcome += data[checkValue][0];
				$( "#outcome" ).removeClass().addClass("fail_by_1");
			} else if (difference < -5 && difference >= -10) {
				outcome += data[checkValue][1];
				$( "#outcome" ).removeClass().addClass("fail_by_2");
			} else if (difference < -10 && difference >= -15) {
				outcome += data[checkValue][2];	
				$( "#outcome" ).removeClass().addClass("fail_by_3");			
			} else if (difference < -15) {
				outcome += data[checkValue][3];
				$( "#outcome" ).removeClass().addClass("fail_by_4");
			}
		}
	}
	
	$( "#outcome" ).html(outcome);
}

function loadData() {
	
	var data = new Array();
	
	data["attack"] = new Array("Hit", "Hit + Multiattack DC+2", "Hit + Multiattack DC+5");
	data["damage"] = new Array("Injury", "Injury + Dazed", "Injury + Staggered", "Injury + Incapacitated");
	data["charm"] = new Array("Injury", "Injury + Entranced", "Injury + Compelled", "Injury + Controlled");
	data["sleep"] = new Array("Injury", "Injury + Fatigued", "Injury + Exhausted", "Injury + Asleep");
	data["paralyze"] = new Array("Injury", "Injury + Hindered", "Injury + Stunned", "Injury + Paralyzed");
	data["weaken"] = new Array("Injury", "Injury + Trait -2", "Injury + Trait -5", "Injury + Trait Fails");
	
	return data;
}