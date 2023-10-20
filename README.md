# dojo-template-godot

## Initial Setup

The repository already contains the `dojo-starter` as a submodule. Feel free to remove it if you prefer.

**Prerequisites:** First and foremost, ensure that Dojo is installed on your system. If it isn't, you can easily get it set up with:

```console
curl -L https://install.dojoengine.org | bash
```

Followed by:

```console
dojoup
```

For an in-depth setup guide, consult the [Dojo book](https://book.dojoengine.org/getting-started/quick-start.html).

## Launch the Example

### Dojo Contract

After cloning the project, execute the following:

0. **init submodule**

```
git submodule update --init --recursive
```

1. **Terminal 1 - Katana**:

```console
cd dojo-starter && katana --disable-fee
```

2. **Terminal 2 - Contracts**:

```console
cd dojo-starter && sozo build && sozo migrate
```

### Connecting to your Dojo Game

This template uses the `dojo-starter` contract as a base. To connect to your game an player, you will need to update the following:

```cs
    private string systemAddress = "SYSTEM_ADDRESS"; // gotten from sozo migrate
	private string playerAddress = "PLAYER_ADDRESS"; // gotten from Katana
	private string playerKey = "PLAYER_PRIVATE_KEY"; // gotten from Katana
```

You can execute a game function by calling the `Execute` method. The first argument is the model name, and the second is an array of arguments.

```cs
void Execute(string model, string[] args)

// Examples

// Spawning a new Player
string[] spwanArgs = new string[] { };
Execute("spawn", spwanArgs); // replace `spawn` with your model name and `spawnArgs` with your arguments

// Moving the Player to the left
string[] moveArgs = new string[] { "1" };
Execute("move", moveArgs);
```

You can also call the `GetData` method to get the current state of the game. The first argument is the model name, and the second is an array of arguments.

```cs
string GetData(string model, string[] args)
```
