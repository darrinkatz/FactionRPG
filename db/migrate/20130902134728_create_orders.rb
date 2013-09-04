class CreateOrders < ActiveRecord::Migration
  def change
    create_table :orders do |t|
      t.string :type
      t.integer :turn_id
      t.integer :asset_id
      t.integer :target_id
      t.string :result

      t.timestamps
    end
  end
end
