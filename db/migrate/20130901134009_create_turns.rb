class CreateTurns < ActiveRecord::Migration
  def change
    create_table :turns do |t|
      t.integer :number
      t.string :state

      t.timestamps
    end
  end
end
