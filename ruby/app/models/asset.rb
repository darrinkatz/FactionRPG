class Asset < ActiveRecord::Base
  belongs_to :faction

  def to_s
    "#{value}-#{name}"
  end
end
