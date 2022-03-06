from statistics import mean
import yaml

from gym_unity.envs import UnityToGymWrapper

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import (
    EngineConfigurationChannel,
)

from PPO import PPO


def main():
    with open("properties.yaml", "r") as stream:
        props = yaml.safe_load(stream)

    timescale = props["engine_config"]["timescale"]

    channel = EngineConfigurationChannel()
    unity_env = UnityEnvironment(side_channels=[channel])

    channel.set_configuration_parameters(time_scale=timescale)

    env = UnityToGymWrapper(unity_env)

    state_dim = env.observation_space.shape[0]
    action_dim = env.action_space.shape[0]

    print("=" * 20)
    print("State Space Size: ", state_dim)
    print("Action Space Size: ", action_dim)
    print("=" * 20)

    model_config = props["model_config"]
    lr_actor = float(model_config["lr_actor"])
    lr_critic = float(model_config["lr_critic"])
    eps_clip = float(model_config["eps_clip"])

    epochs = int(model_config["epochs"])

    update_episodes = int(model_config["update_episodes"])
    episode_len = float(model_config["max_episode_len"])
    decision_frequency = float(model_config["decision_frequency"])

    episode_steps = int(episode_len * decision_frequency)

    gamma = float(model_config["gamma"])
    action_std_init = float(model_config["action_std_init"])

    swap_episodes = int(model_config["swap_episodes"])

    # ppo_agent = PPO(
    #     state_dim,
    #     action_dim,
    #     lr_actor,
    #     lr_critic,
    #     gamma,
    #     epochs,
    #     eps_clip,
    #     has_continuous_action_space=True,
    #     action_std_init=action_std_init,
    # )

    i_episode = 1

    while True:
        state = env.reset()
        current_ep_reward = 0

        for t in range(1, episode_steps + 1):
            # action = ppo_agent.select_action(state)
            # state, reward, done, _ = env.step(action)
            state, reward, done, _ = env.step(env.action_space.sample())

            # ppo_agent.buffer.rewards.append(reward)
            # ppo_agent.buffer.is_terminals.append(done)

            current_ep_reward += reward

            if done:
                break

        print(f"Episode: {i_episode}\tTimesteps: {t}\tReward: {current_ep_reward}")

        if i_episode % update_episodes == 0:
            print("Training Model")
            # losses = ppo_agent.update()
            # print(f"Loss: {mean(losses)}")

        current_ep_reward = 0
        i_episode += 1


if __name__ == "__main__":
    main()
