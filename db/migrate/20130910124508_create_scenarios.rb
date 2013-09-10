class CreateScenarios < ActiveRecord::Migration
  def change
    create_table :scenarios do |t|
      t.integer :faction_id
      t.integer :turn_id

      t.timestamps
    end
  end
end
