class TurnProcessor
  def initialize turn
    @turn = turn
  end

  def calculate
    @turn.orders.each do |order|
      order.calculate_result
    end
    @turn
  end

  def finalize
    calculate.save
    @turn
  end
end
