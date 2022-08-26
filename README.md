# AutoBrew

A plugin for PotionCraft 0.5.0, powered by BepInEx. Automatically brews a potion from JSON data.

## Installation

1) Download the latest version of [BepInEx 5.x 64bit](https://github.com/BepInEx/BepInEx/releases), and extract the all files to the directory where PotionCraft.exe resides
2) Run the game once to allow BepInEx to create configs and folders
3) Download the latest release of [AutoBrew](https://github.com/rswallen/AutoBrew/releases) and extract the contents to the newly created `/BepInEx/plugins/` folder

## Usage

1) Open the custom description panel in PotionCraft, paste in your JSON code and close using the green `OK` button.
2) Type `AutoBrew` in the custom title box and press enter to tell the plugin to begin brewing.

## JSON Format

Valid JSON data takes the form of an array of collections of key value pairs, with each collection representing a single brewing instruction to the plugin.
There are 6 different instructions currently available: `AddIngredient`, `StirCauldron`, `HeatVortex`, `PourSolvent`, `AddSalt` and `AddEffect`.

**Example of AddIngredient:**

The `AddIngredient` instruction requires 3 values: the order, an item (the ingredient) and a target (used for grinding):

        {
            "order": "AddIngredient",
            "item": "Mudshroom",
            "target": "100.0"
        }

**Example of StirCauldron:**

The `StirCauldron` instruction requires only 2 values: the order and a target (the amount to stir):

    {
        "order": "StirCauldron",
        "target": "7.8"
    }

**Example of HeatVortex:**

The `HeatVortex` instruction requires 2 values (the order and target), but accepts a 3rd, a version number. The value of the third argument determines how the plugin interprets the target. If no version value is provided, the plugin interprets the target value as an angle in degrees, but if the value is 1 or 0, the plugin will interpret the target value as a distance as measured by the potionous website (0 and 1 being two different versions of the measurement system):

        {
            "order": "HeatVortex",
            "target": "12.0",
            "version": "1"
        }

**Example of PourSolvent:**

The `PourSolvent` instruction is indentical to the `StirCauldron` instruction, in that it only requires 2 values: the order and the target (though this time, target denotes how much to base pour into the potion):

    {
        "order": "PourSolvent",
        "target": "10.0"
    }

**Example of AddEffect:**

The `AddEffect` instruction requires only 1 value (the order), but will accept a target value too (the target denoting the desired tier). The target can be 0, 1, 2, 3 (can't recall if this actually has any impact atm):

    {
        "order": "AddEffect",
        "target": "3"
    }

**Example of AddSalt:**

The `AddSalt` instruction takes 3 arguments: the order, an item (the name of the salt to add) and a target (the integer amount of the salt to add):

    {
        "order": "AddSalt",
        "item": "Moon Salt",
        "target": "100"
    }

**Example of complete JSON data:**

To create a method, simply stack any number of these instructions blocks, add a comma between each and enclose in square brackets to turn it into an array that the JSON parser can read.

Example that creates a strength 3 potion on the water base:

    [
        {
            "order": "AddIngredient",
            "item": "Mudshroom",
            "target": "100.0"
        },
        {
            "order": "StirCauldron",
            "target": "7.8"
        },
        {
            "order": "HeatVortex",
            "target": "12.0",
            "version": "1"
        },
        {
            "order": "StirCauldron",
            "target": "4.93"
        },
        {
            "order": "AddEffect",
            "target": "3"
        },
        {
            "order": "PourSolvent",
            "target": "10.0"
        }
    ]