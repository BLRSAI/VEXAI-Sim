from mlagents_envs.environment import UnityEnvironment


class UnsupportedEnviornment(Exception):
    pass


class MLAgentsWrapper:
    def __init__(self, unity_env: UnityEnvironment):
        # most of this just comes from the unity mlagents gym wrapper source code
        # it doesn't have most of the features of the original implementation
        # but it has multi agent support

        self.env = unity_env

        if not self.env.behavior_specs:
            self.env.step()

        self.game_over = False

        self.num_agents = len(self.env.behavior_specs)
        self.agent_names = self.env.behavior_specs.keys()

        if self.num_agents != 2:
            raise UnsupportedEnviornment("Expected 2 agents, found {}".format(self.num_agents))


if __name__ == "__main__":
    unity_env = UnityEnvironment()
    env = MLAgentsWrapper(unity_env)
    print(env.agent_names)

    pass
