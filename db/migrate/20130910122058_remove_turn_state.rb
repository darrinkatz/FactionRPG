class RemoveTurnState < ActiveRecord::Migration
  def change
  	remove_column :turns, :state
  end
end
