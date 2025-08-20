import type { Meta, StoryObj } from '@storybook/react-webpack5';
import { ChatInput } from '../components/ChatInput';

const meta = {
  title: 'Components/ChatInput',
  component: ChatInput,
  parameters: {
    layout: 'centered',
  },
  decorators: [
    (Story) => (
      <div style={{ width: '600px', border: '1px solid #e5e7eb', borderRadius: '8px' }}>
        <Story />
      </div>
    ),
  ],
  tags: ['autodocs'],
  argTypes: {
    onSendMessage: { action: 'message sent' },
  },
} satisfies Meta<typeof ChatInput>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    onSendMessage: (message: string) => console.log('Message sent:', message),
    isLoading: false,
  },
};

export const Loading: Story = {
  args: {
    onSendMessage: (message: string) => console.log('Message sent:', message),
    isLoading: true,
  },
};