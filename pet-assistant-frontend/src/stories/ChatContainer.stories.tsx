import type { Meta, StoryObj } from '@storybook/react-webpack5';
import { ChatContainer } from '../components/ChatContainer';

const meta = {
  title: 'Components/ChatContainer',
  component: ChatContainer,
  parameters: {
    layout: 'fullscreen',
  },
  tags: ['autodocs'],
} satisfies Meta<typeof ChatContainer>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
};

export const WithMockedAPI: Story = {
  args: {},
  parameters: {
    mockData: [
      {
        url: 'http://localhost:5293/api/chat',
        method: 'POST',
        status: 200,
        response: {
          response: 'I understand your concern about your pet. Based on your description, I recommend monitoring the situation closely. If symptoms persist or worsen, please consult with a veterinarian for proper diagnosis and treatment.',
          thinking: 'The user seems concerned about their pet. I should provide general advice while emphasizing the importance of professional veterinary care.',
          sessionId: 'mock-session-123',
          timestamp: new Date().toISOString(),
        },
        delay: 1000,
      },
    ],
  },
};