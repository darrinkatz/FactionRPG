class FactionsController < ApplicationController

	def index

		@factions = Faction.all

	end

	def new

		@faction = Faction.new

	end

	def create

		@faction = Faction.create( app_params )

	end

	private

	def app_params

		params.require(:faction).permit(:faction_name, :player_email)

	end

end
