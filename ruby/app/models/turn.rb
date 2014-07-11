class Turn < ActiveRecord::Base
  has_many :orders
  has_many :assets, through: :orders

  accepts_nested_attributes_for :orders

  scope :in_progress, -> { where(state: "in_progress") }

  def self.current
    in_progress.first
  end

  def self.start_next_turn
    turn = self.create(state: :in_progress, number: self.count + 1)
    Asset.all.each do |asset|
      turn.orders.create(asset: asset, type: "Attack")
    end
    turn
  end
end
