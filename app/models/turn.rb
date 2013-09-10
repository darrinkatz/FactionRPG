class Turn < ActiveRecord::Base
  has_many :orders
  has_many :assets, through: :orders

  accepts_nested_attributes_for :orders

  def self.current
    self.last
  end

  def self.start_next_turn
    turn = self.create(number: self.count + 1)
    Asset.all.each do |asset|
      turn.orders.create(asset: asset, type: "Attack")
    end
    turn
  end
end
