class CreateAssets < ActiveRecord::Migration
  def change
    create_table :assets do |t|
      t.string :name
      t.integer :value
      t.boolean :covert
      t.integer :faction_id

      t.timestamps
    end
  end
end
