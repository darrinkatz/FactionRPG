FactionRPG::Application.routes.draw do

  resources :factions do
    resources :assets
  end

end
