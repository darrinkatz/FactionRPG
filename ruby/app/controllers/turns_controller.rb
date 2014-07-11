class TurnsController < ApplicationController

  def create
    @turn = Turn.start_next_turn
    redirect_to factions_path
  end

  def edit
    @turn = Turn.current
  end

  def update
    turn = Turn.find params[:id]
    turn.update_attributes turn_params
    redirect_to factions_path
  end

  def finalize
    @turn = TurnProcessor.new(Turn.current).finalize
  end

  private

  def turn_params
    params.require(:turn).permit(orders_attributes: [:type, :target_id, :id])
  end
end
