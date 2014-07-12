function onFailed() {
    $get('divPerspective').innerHTML = 'Something went terribly wrong!';
}

function drawGame(e) {
    if (e.length > 0) {
        $('body').data('perspective', Sys.Serialization.JavaScriptSerializer.deserialize(e, true));
        $get('divPerspective').innerHTML = renderPerspective();

        $(".actor").dblclick(function () {
            renderViewProfile($(this).attr('id'));
            $("#view-profile").dialog({
                modal: true
            });
        });

        $(".actor").draggable({
            revert: 'invalid',
            opacity: 0.5,
            helper: 'clone',
            revertDuration: 250,
            start: function () {
                $(".dropzone").show().droppable({
                    greedy: true,
                    over: function (event, ui) { $(this).addClass("actor-highlight"); },
                    out: function (event, ui) { $(this).removeClass("actor-highlight"); },
                    drop: function (event, ui) {
                        var action;
                        if ($(this).hasClass('dropzone-damage')) {
                            action = 'Damage';
                        }
                        if ($(this).hasClass('dropzone-enhance')) {
                            action = 'Enhance';
                        }
                        if ($(this).hasClass('dropzone-shroud')) {
                            action = 'Shroud';
                        }
                        if ($(this).hasClass('dropzone-infiltrate')) {
                            action = 'Infiltrate';
                        }
                        updateAssets(getQuery('guid'), ui.draggable.attr('id'), $(this).parent().attr('id'), action);
                    }
                });
            },
            stop: function () {
                $(".dropzone").hide().unbind("droppable");
            }
        });

//        $(".troupe").droppable({
//            over: function (event, ui) { $(this).addClass("actor-highlight"); },
//            out: function (event, ui) { $(this).removeClass("actor-highlight"); },
//            drop: function (event, ui) {
//                updateAssets(
//                    getQuery('guid'),
//                    ui.draggable.attr('id'),
//                    $(this).data('targetid'),
//                    $(this).data('action')
//                );
//            }
//        });
    }
}

function loadGame(guid, turn) {
    GameService.GetGame(guid, turn, loadGame_success, onFailed);
}

function loadGame_success(e) {
    drawGame(e);
}

function updateAssets(guid, assetId, targetId, action) {
    GameService.UpdateAssets(guid, assetId, targetId, action, updateAssets_success, onFailed);
}

function updateAssets_success(e) {
    drawGame(e)
}