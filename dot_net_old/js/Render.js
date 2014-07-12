function getActor(actorId) {

    var actorInfo;
    for (var actor in $('body').data('perspective').actors) {
        if ($('body').data('perspective').actors[actor].id == actorId) {
            actorInfo = $('body').data('perspective').actors[actor];
        }
    }

    return actorInfo;
}

function renderViewProfile(actorId) {

    var actorInfo = getActor(actorId);

    var viewProfileHtml = new Sys.StringBuilder();

    viewProfileHtml.append("<ul>");

    viewProfileHtml.append("<li>");
    viewProfileHtml.append(renderActor(actorId));
    viewProfileHtml.append("</li>");

    if (actorInfo.isSentinel == false) {
        viewProfileHtml.append("<li class=\"setSentinel\">");
        viewProfileHtml.append(String.format("Set {0} as Sentinel", actorInfo.name));
        viewProfileHtml.append("</li>");
    }

    if (actorInfo.covert == true) {
        viewProfileHtml.append("<li class=\"shareProfile\">");
        viewProfileHtml.append(String.format("<p>Share profile on {0} with...</p>", actorInfo.name));
        viewProfileHtml.append("</li>");
    }

    viewProfileHtml.append("<li class=\"changeOrder\">");
    viewProfileHtml.append(String.format("<p>Order {0} to target (target) with (action).</p>", actorInfo.name));
    viewProfileHtml.append("</li>");

    viewProfileHtml.append("</ul>");

    $get('view-profile').innerHTML = viewProfileHtml;
}

function renderPerspective() {

    var perspectiveHtml = new Sys.StringBuilder();

    for (var scene in $('body').data('perspective').scenes) {
        perspectiveHtml.append(renderScene($('body').data('perspective').scenes[scene]));
    }

    return perspectiveHtml;
}

function renderScene(scene) {

    var sceneHtml = new Sys.StringBuilder();

    sceneHtml.append("<div class=\"row scene\">");

    sceneHtml.append("<div class=\"span19 scene-header\">");
    sceneHtml.append("<div class=\"row\">");
    sceneHtml.append("<div class=\"span3\">");
    sceneHtml.append("<h2>&nbsp;Target</h2>");
    sceneHtml.append("</div>");
    sceneHtml.append("<div class=\"span1\">");
    sceneHtml.append("<h2>With</h2>");
    sceneHtml.append("</div>");
    sceneHtml.append("<div class=\"span15\">");
    sceneHtml.append("<h2>&nbsp;By</h2>");
    sceneHtml.append("</div>");
    sceneHtml.append("</div>");
    sceneHtml.append("</div>");

    var first = true;
    for (var troupe in scene.troupes) {
        sceneHtml.append(renderTroupe(scene.targetId, scene.troupes[troupe], first));
        if (first == true) {
            first = false;
        }
    }

    sceneHtml.append("</div>");

    return sceneHtml;
}

function renderTroupe(targetId, troupe, first) {

    var troupeHtml = new Sys.StringBuilder();

    var rowCount = (troupe.actorIds.length - 1) / 5 + 1;

    for (var currentRow = 1; currentRow <= rowCount; currentRow++) {

        troupeHtml.append("<div class=\"span19 troupe\">");
        troupeHtml.append("<div class=\"row\">");

        troupeHtml.append("<div class=\"span3 target\">");
        if (first == true && currentRow == 1) {
            troupeHtml.append(renderActor(targetId));
        }
        troupeHtml.append("</div>");

        troupeHtml.append("<div class=\"span1 action-header\">");
        if (currentRow == 1) {
            troupeHtml.append("<span class=\"strength\">");
            troupeHtml.append(troupe.strength);
            troupeHtml.append("</span>");

            var imgSrc;

            if (troupe.action == "Damage") {
                imgSrc = '/img/glyphicons/glyphicons_022_fire.png';
            }
            if (troupe.action == "Enhance") {
                imgSrc = '/img/glyphicons/glyphicons_280_settings.png';
            }
            if (troupe.action == "Shroud") {
                imgSrc = '/img/glyphicons/glyphicons_052_eye_close.png';
            }
            if (troupe.action == "Infiltrate") {
                imgSrc = '/img/glyphicons/glyphicons_204_unlock.png';
            }

            troupeHtml.append(String.format("<img src=\"{0}\" alt=\"{1}\">", imgSrc, troupe.action));

            troupeHtml.append("<span class=\"specializations\">");
            if (troupe.specializationAccess.indexOf("Disguise") != -1) { troupeHtml.append("<i class=\"icon-user\"></i>"); }
            if (troupe.action == "Shroud") {
                if (troupe.specializationAccess.indexOf("Ambush") != -1) { troupeHtml.append("<i class=\"icon-screenshot\"></i>"); } 
            }
            if (troupe.action == "Enhance") {
                if (troupe.specializationAccess.indexOf("Infuse") != -1) { troupeHtml.append("<i class=\"icon-tint\"></i>"); }
                if (troupe.specializationAccess.indexOf("Propagate") != -1) { troupeHtml.append("<i class=\"icon-signal\"></i>"); }
            }
            if (troupe.action == "Damage") {
                if (troupe.specializationAccess.indexOf("Maneuver") != -1) { troupeHtml.append("<i class=\"icon-random\"></i>"); }
                if (troupe.specializationAccess.indexOf("Salvage") != -1) { troupeHtml.append("<i class=\"icon-refresh\"></i>"); }
            }
            if (troupe.action == "Infiltrate") {
                if (troupe.specializationAccess.indexOf("Investigate") != -1) { troupeHtml.append("<i class=\"icon-search\"></i>"); }
                if (troupe.specializationAccess.indexOf("Vanish") != -1) { troupeHtml.append("<i class=\"icon-adjust\"></i>"); } 
            }
            troupeHtml.append("</span>");
        }
        troupeHtml.append("</div>");

        for (var actorIndex in troupe.actorIds) {
            if (actorIndex >= ((currentRow - 1) * 5) && actorIndex <= ((currentRow * 5) - 1)) {
                troupeHtml.append(renderActor(troupe.actorIds[actorIndex]));
            } 
        }

        troupeHtml.append("</div>");
        troupeHtml.append("</div>");
    }

    return troupeHtml;
}

function renderActor(actorId) {

    var actorInfo = getActor(actorId);

    var actorHtml = new Sys.StringBuilder();

    actorHtml.append(
        String.format("<div id=\"{0}\" class=\"span3 actor{1}\" style=\"background:#{2}\">",
            actorInfo.id,
            actorInfo.covert == true ? " covert" : "",
            actorInfo.factionHexColour
        )
    );

    actorHtml.append("<div class=\"value\">");
    actorHtml.append(String.format("<span>{0}</span>", actorInfo.value));
    if (actorInfo.isSentinel == true) {
        actorHtml.append("<span class=\"sentinel\"><img src=\"/img/glyphicons/glyphicons_270_shield.png\" alt=\"Sentinel\" /></span>");
    }
    actorHtml.append("</div>");

    actorHtml.append("<div class=\"status-icons\">");
    if (actorInfo.expectedValueChange > 0) { actorHtml.append("<i class=\"icon-plus-sign\"></i>"); }
    if (actorInfo.valueLossFromDamage > 0) { actorHtml.append("<i class=\"icon-fire\"></i>"); }
    if (actorInfo.expectedValueChange < 0) { actorHtml.append("<i class=\"icon-minus-sign\"></i>"); }
    if (actorInfo.wasEnhanced == true) { actorHtml.append("<i class=\"icon-wrench\"></i>"); }
    if (actorInfo.wasShrouded == true) { actorHtml.append("<i class=\"icon-eye-close\"></i>"); }
    actorHtml.append("</div>");

    actorHtml.append(String.format("<div class=\"avatar\" style=\"background:#{0}\">", actorInfo.factionHexColour));
    //actorHtml.append(String.format("<img src=\"http://placehold.it/100&text={0}\" width=\"100%\" />", actorInfo.name));
    actorHtml.append(String.format("<img src=\"{0}\" width=\"100%\" alt=\"{1}\" />", actorInfo.imageUrl, actorInfo.name));
    actorHtml.append("</div>");

    actorHtml.append("<div class=\"special-icons\">");
    if (actorInfo.hasAmbush == true) { actorHtml.append("<i class=\"icon-screenshot\"></i>"); }
    if (actorInfo.hasDisguise == true) { actorHtml.append("<i class=\"icon-user\"></i>"); }
    if (actorInfo.hasInfuse == true) { actorHtml.append("<i class=\"icon-tint\"></i>"); }
    if (actorInfo.hasInvestigate == true) { actorHtml.append("<i class=\"icon-search\"></i>"); }
    if (actorInfo.hasManeuver == true) { actorHtml.append("<i class=\"icon-random\"></i>"); }
    if (actorInfo.hasPropagate == true) { actorHtml.append("<i class=\"icon-signal\"></i>"); }
    if (actorInfo.hasSalvage == true) { actorHtml.append("<i class=\"icon-refresh\"></i>"); }
    if (actorInfo.hasVanish == true) { actorHtml.append("<i class=\"icon-adjust\"></i>"); }

    if (actorInfo.infusedWithAmbush == true) { actorHtml.append("<i class=\"icon-screenshot icon-white\"></i>"); }
    if (actorInfo.infusedWithDisguise == true) { actorHtml.append("<i class=\"icon-user icon-white\"></i>"); }
    if (actorInfo.infusedWithInfuse == true) { actorHtml.append("<i class=\"icon-tint icon-white\"></i>"); }
    if (actorInfo.infusedWithInvestigate == true) { actorHtml.append("<i class=\"icon-search icon-white\"></i>"); }
    if (actorInfo.infusedWithManeuver == true) { actorHtml.append("<i class=\"icon-random icon-white\"></i>"); }
    if (actorInfo.infusedWithPropagate == true) { actorHtml.append("<i class=\"icon-signal icon-white\"></i>"); }
    if (actorInfo.infusedWithSalvage == true) { actorHtml.append("<i class=\"icon-refresh icon-white\"></i>"); }
    if (actorInfo.infusedWithVanish == true) { actorHtml.append("<i class=\"icon-adjust icon-white\"></i>"); }
    actorHtml.append("</div>");

    actorHtml.append('<div class="dropzone dropzone-shroud"></div>');
    actorHtml.append('<div class="dropzone dropzone-enhance" ></div>');
    actorHtml.append('<div class="dropzone dropzone-damage" ></div>');
    actorHtml.append('<div class="dropzone dropzone-infiltrate" ></div>');

    actorHtml.append("</div>");

    return actorHtml;
}