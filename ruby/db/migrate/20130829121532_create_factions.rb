class CreateFactions < ActiveRecord::Migration
  def change
    create_table :factions do |t|
      t.string :faction_name
      t.string :player_email

      t.timestamps
    end
  end
end
