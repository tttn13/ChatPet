import type { Meta, StoryObj } from '@storybook/react-webpack5';
import { BrowserRouter } from 'react-router-dom';
import { ChatActions } from '../components/ChatActions';

const meta = {
  title: 'Components/ChatActions',
  component: ChatActions,
  parameters: {
    layout: 'centered',
  },
  decorators: [
    (Story) => (
      <BrowserRouter>
        <div style={{ padding: '20px', backgroundColor: '#f9fafb' }}>
          <Story />
        </div>
      </BrowserRouter>
    ),
  ],
  tags: ['autodocs'],
  argTypes: {
    onClearChat: { action: 'clear chat' },
  },
} satisfies Meta<typeof ChatActions>;

export default meta;
type Story = StoryObj<typeof meta>;

export const WithMessages: Story = {
  args: {
    onClearChat: () => console.log('Clear chat clicked'),
    messagesCount: 15,
  },
};

export const EmptyChat: Story = {
  args: {
    onClearChat: () => console.log('Clear chat clicked'),
    messagesCount: 0,
  },
};

export const FewMessages: Story = {
  args: {
    onClearChat: () => console.log('Clear chat clicked'),
    messagesCount: 3,
  },
};