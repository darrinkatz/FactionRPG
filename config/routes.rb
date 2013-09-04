FactionRPG::Application.routes.draw do

  resources :factions do
    resources :assets
  end

  resources :turns do
    member do
      patch "finalize"
    end
  end

end
