function extractAssets(turn)
{
	var turnObj = JSON.parse(turn);
	var faction = turnObj["perspective"];
	var assets = turnObj["assets"];
	
	var result = JSON.parse(turn);
	result["assets"] = new Array();
	
	for (i = 0; i < assets.length; i++)
	{
		if (turnObj["assets"][i]["faction"] == faction)
		{
			result["assets"].push(turnObj["assets"][i]);
		}
	}

	return JSON.stringify(result);
}

function mapFromJsonToVisualizer(turn)
{
	var turnObj = JSON.parse(turn);
	var assets = turnObj["assets"];

	return JSON.stringify(assets);
}

function mapFromVisualizerToJson(visualizerData, turn)
{
	var turnObj = JSON.parse(turn);
	turnObj["assets"] = JSON.parse(visualizerData);

	return JSON.stringify(turnObj);
}