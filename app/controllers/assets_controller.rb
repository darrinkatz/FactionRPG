class AssetsController < ApplicationController

  def new
    @faction = Faction.find params[:faction_id]
    @asset = @faction.assets.new
  end

  def create
    @faction = Faction.find params[:faction_id]
    @faction.assets.create asset_params

    render "factions/create"
  end

  private

  def asset_params
    params.require(:asset).permit(:name, :value, :covert)
  end
end
