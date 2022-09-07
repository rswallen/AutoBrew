using Newtonsoft.Json;

namespace AutoBrew.JsonObjects
{
    internal class PlotterOrder : JsonOrder
    {
        [JsonProperty("type")]
        public string Type
        {
            get => Stage;
            set
            {
                switch (value)
                {
                    case "add-ingredient":
                    {
                        Stage = "AddIngredient";
                        break;
                    }
                    case "stir-cauldron":
                    {
                        Stage = "StirCauldron";
                        break;
                    }
                    case "pour-solvent":
                    {
                        Stage = "PourSolvent";
                        break;
                    }
                    case "heat-vortex":
                    {
                        Stage = "HeatVortex";
                        break;
                    }
                    case "add-rotation-salt":
                    {
                        Stage = "AddSalt";

                        break;
                    }
                    case "void-salt":
                    {
                        Stage = "AddSalt";
                        Item = "Void Salt";
                        break;
                    }
                }
            }
        }

        [JsonProperty("ingredientId")]
        public string Ingredient
        {
            get => Item;
            set => Item = value;
        }

        [JsonProperty("salt")]
        public string Salt
        {
            get => Item;
            set
            {
                switch (value)
                {
                    case "sun":
                    {
                        Item = "Sun Salt";
                        break;
                    }
                    case "moon":
                    {
                        Item = "Moon Salt";
                        break;
                    }
                }
            }
        }

        [JsonProperty("grindPercent")]
        public double GrindPercent
        {
            get => Target;
            set => Target = value;
        }

        [JsonProperty("distance")]
        public double Distance
        {
            get => Target;
            set => Target = value;
        }

        [JsonProperty("grains")]
        public double Grains
        {
            get => Target;
            set => Target = value;
        }

        [JsonProperty("x")]
        public float X
        {
            get { return 0; }
            set { }
        }

        [JsonProperty("y")]
        public float Y
        {
            get { return 0; }
            set { }
        }
    }
}